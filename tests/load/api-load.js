// k6 load test for the API: a catalog-browsing scenario and a checkout scenario run side by side.
//
//   docker compose up -d --build --wait
//   k6 run tests/load/api-load.js
//
// Configure with environment variables (k6 -e NAME=value): BASE_URL, TOKEN_URL, CLIENT_ID, CLIENT_SECRET,
// DURATION, BROWSE_RATE and CHECKOUT_RATE. The defaults target docker-compose.yml and its DEV ONLY Keycloak client.
import http from 'k6/http';
import { check, fail } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const TOKEN_URL = __ENV.TOKEN_URL || 'http://localhost:8180/realms/clean-architecture/protocol/openid-connect/token';
const CLIENT_ID = __ENV.CLIENT_ID || 'clean-architecture-service';
const CLIENT_SECRET = __ENV.CLIENT_SECRET || 'dev-only-service-secret';
const DURATION = __ENV.DURATION || '1m';
const SEARCH_TERMS = ['keyboard', 'mouse', 'monitor', 'cable', 'headset'];

export const options = {
  scenarios: {
    browse: {
      executor: 'constant-arrival-rate',
      exec: 'browse',
      rate: Number(__ENV.BROWSE_RATE || 40),
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: 20,
      maxVUs: 100,
    },
    checkout: {
      executor: 'constant-arrival-rate',
      exec: 'checkout',
      rate: Number(__ENV.CHECKOUT_RATE || 5),
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: 10,
      maxVUs: 50,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{scenario:browse}': ['p(95)<300'],
    'http_req_duration{scenario:checkout}': ['p(95)<800'],
    checks: ['rate>0.99'],
  },
};

function randomItem(items) {
  return items[Math.floor(Math.random() * items.length)];
}

function authHeaders(token) {
  return { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } };
}

export function setup() {
  const tokenResponse = http.post(TOKEN_URL, {
    grant_type: 'client_credentials',
    client_id: CLIENT_ID,
    client_secret: CLIENT_SECRET,
  });
  if (tokenResponse.status !== 200) {
    fail(`Could not get an access token from ${TOKEN_URL}: ${tokenResponse.status} ${tokenResponse.body}`);
  }
  const token = tokenResponse.json('access_token');

  // Seed a catalog with stock to spare for the whole run; SKUs are unique per run.
  const run = Date.now();
  const productIds = SEARCH_TERMS.flatMap((term, termIndex) =>
    [1, 2, 3, 4].map((variant) => {
      const response = http.post(
        `${BASE_URL}/api/products`,
        JSON.stringify({
          name: `Load ${term} ${variant}`,
          description: `Seeded ${term} for load testing`,
          price: 10 + termIndex * 25 + variant,
          currency: 'USD',
          sku: `LOAD-${run}-${termIndex}-${variant}`,
          stockQuantity: 1000000,
        }),
        authHeaders(token),
      );
      if (response.status !== 201) {
        fail(`Seeding failed: ${response.status} ${response.body}`);
      }
      return response.json();
    }),
  );

  return { token, productIds };
}

export function browse(data) {
  const params = authHeaders(data.token);
  const list = http.get(
    `${BASE_URL}/api/products?search=${randomItem(SEARCH_TERMS)}&sort=${randomItem(['name', '-price'])}&pageSize=20`,
    Object.assign({ tags: { name: 'GET /api/products' } }, params),
  );
  check(list, { 'list 200': (response) => response.status === 200 });

  const single = http.get(
    `${BASE_URL}/api/products/${randomItem(data.productIds)}`,
    Object.assign({ tags: { name: 'GET /api/products/{id}' } }, params),
  );
  check(single, { 'get 200': (response) => response.status === 200 });
}

export function checkout(data) {
  const params = authHeaders(data.token);
  const order = http.post(
    `${BASE_URL}/api/orders`,
    JSON.stringify({
      lines: [
        { productId: randomItem(data.productIds), quantity: 1 },
        { productId: randomItem(data.productIds), quantity: 2 },
      ],
    }),
    Object.assign(
      {
        tags: { name: 'POST /api/orders' },
        // 409 is a legitimate optimistic-concurrency outcome when two orders reserve the same product at once.
        responseCallback: http.expectedStatuses(201, 409),
      },
      params,
    ),
  );
  const placed = check(order, { 'order 201 or 409': (response) => response.status === 201 || response.status === 409 });
  if (!placed || order.status !== 201) {
    return;
  }

  const pay = http.post(
    `${BASE_URL}/api/orders/${order.json()}/pay`,
    null,
    Object.assign({ tags: { name: 'POST /api/orders/{id}/pay' } }, params),
  );
  check(pay, { 'pay 204': (response) => response.status === 204 });
}
