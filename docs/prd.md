# Product Requirements Document

> The requirements this project was built from, kept verbatim. Decisions made while implementing
> them are recorded in [decisions/](decisions/).

## 1. Project Overview

Build a lightweight web-based API gateway that allows API owners to register APIs, define usage tiers, issue API keys, enforce rate limits, and monitor API consumption.

The project is intended to demonstrate core API gateway concepts such as API-key authentication, Redis-based rate limiting, usage metering, quotas, analytics, and simple credit-based billing.

## 2. Objectives

The system should allow:

- API owners to register and manage APIs.
- API owners to define access tiers with rate limits and monthly quotas.
- Consumers to receive API keys and access registered APIs through the gateway.
- The gateway to validate API keys and enforce configured rate limits.
- API usage to be recorded and displayed through basic dashboards.
- Consumers to track usage, remaining quota, and mock credit balance.
- Webhook notifications to be triggered when configured usage limits are reached.

## 3. User Roles

### API Owner
Can register APIs, create tiers, generate/revoke API keys, view consumers, configure limits, and monitor usage.

### API Consumer
Can view assigned API keys, usage statistics, quota consumption, and remaining credits.

## 4. Core Features

### API Registration
Owners can register an API using a name, description, and target base URL.

### Tier Management
Owners can configure tiers containing:

- Requests per minute
- Monthly request quota
- Credit cost per request

### API Key Management
Owners can generate and revoke API keys and assign them to consumers and tiers.

### API Gateway
Consumers send requests through the gateway using an API key. The gateway validates the key, checks limits, forwards allowed requests to the target API, and records usage.

### Rate Limiting
Use Redis with a fixed-window rate-limiting algorithm. Requests exceeding the configured limit return HTTP `429`.

### Usage Analytics
Track:

- Total requests
- Successful requests
- Failed requests
- Rate-limited requests
- Usage per consumer
- Credits consumed

### Mock Billing
Consumers have a virtual credit balance. Successful API requests deduct credits according to the assigned tier.

No real payment integration is required.

### Webhook Alerts
Allow configuration of a webhook URL for events such as quota threshold reached or quota exceeded.

## 5. Suggested Technology

- Frontend: React + TypeScript
- Backend: C#
- for api docs - swagger
- Database: MySQL
- Rate Limiting / Cache: Redis
- Deployment: Docker Compose

## 6. Out of Scope

The project will not include real payments, OAuth/OIDC, Kubernetes, microservices, distributed tracing, advanced billing, complex RBAC, multi-region deployments, or production-grade API management features.

## 7. Success Criteria

A successful demo should show an API owner creating an API and tier, issuing an API key, a consumer making requests through the gateway, rate limits being enforced, and usage appearing on the dashboard.

## Additional constraints

- `SOLID` and `DRY` must always be kept in mind.
- Document all the decisions made in the `docs/` folder.
- One top-level folder containing `backend/`, `frontend/`, `docs/`, etc.
