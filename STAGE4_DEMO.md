# Stage 4 Demo Guide

## What is included
- `mybroker` - main broker API (ASP.NET Core)
- `producer` - test microservice that creates topic and publishes events
- `consumer-a` - test microservice that subscribes and consumes events
- `consumer-b` - test microservice that subscribes and consumes events

All three test microservices write detailed event logs to container stdout.

## Run demo environment
```bash
docker compose -f docker-compose.yml -f docker-compose.demo.yml up -d --build
```

## Watch logs (main proof for final demo)
```bash
docker compose -f docker-compose.yml -f docker-compose.demo.yml logs -f producer consumer-a consumer-b
```

Expected result:
- producer logs `topic created` and `published id=... payload=event-N`
- each consumer logs `received #N/10 ...`
- each consumer finishes with `done received_all=10`

## Check broker API / Swagger
- `http://localhost:8000/swagger`

## Stop and clean demo environment
```bash
docker compose -f docker-compose.yml -f docker-compose.demo.yml down -v
```
