# Benchmark Summary

| Name | Category | Broker | Size | Rate | Sent | Processed | Lost | Backlog | Errors | msg/sec | avg ms | p95 ms | max ms | degraded |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| baseline-rabbitmq | baseline | RabbitMq | 1024 B | 1000/sec | 0 | 0 | 0 | 0 | 1 | 0,00 | 0,00 | 0,00 | 0,00 | True |
| baseline-redis | baseline | Redis | 1024 B | 1000/sec | 5000 | 5000 | 0 | 0 | 0 | 1000,00 | 32,68 | 41,04 | 1054,57 | False |
| size-128b-rabbitmq | payload-size | RabbitMq | 128 B | 3000/sec | 15000 | 15000 | 0 | 0 | 0 | 3000,00 | 8,84 | 16,47 | 30,64 | False |
| size-128b-redis | payload-size | Redis | 128 B | 3000/sec | 15000 | 15000 | 0 | 0 | 0 | 3000,00 | 13,62 | 20,43 | 1077,47 | False |
| size-1kb-rabbitmq | payload-size | RabbitMq | 1024 B | 3000/sec | 15000 | 15000 | 0 | 0 | 0 | 3000,00 | 13,61 | 35,52 | 86,98 | False |
| size-1kb-redis | payload-size | Redis | 1024 B | 3000/sec | 15000 | 15000 | 0 | 0 | 0 | 3000,00 | 11,10 | 17,02 | 1082,30 | False |
| size-10kb-rabbitmq | payload-size | RabbitMq | 10240 B | 1000/sec | 5000 | 5000 | 0 | 0 | 0 | 1000,00 | 9,91 | 22,12 | 51,67 | False |
| size-10kb-redis | payload-size | Redis | 10240 B | 1000/sec | 5000 | 5000 | 0 | 0 | 0 | 1000,00 | 22,30 | 29,89 | 1039,99 | False |
| size-100kb-rabbitmq | payload-size | RabbitMq | 102400 B | 100/sec | 500 | 500 | 0 | 0 | 0 | 100,00 | 11,33 | 27,81 | 64,37 | False |
| size-100kb-redis | payload-size | Redis | 102400 B | 100/sec | 500 | 500 | 0 | 0 | 0 | 100,00 | 121,69 | 225,89 | 1106,11 | False |
| rate-1k-rabbitmq | stream-rate | RabbitMq | 1024 B | 1000/sec | 5000 | 5000 | 0 | 0 | 0 | 1000,00 | 6,47 | 9,33 | 24,22 | False |
| rate-1k-redis | stream-rate | Redis | 1024 B | 1000/sec | 5000 | 5000 | 0 | 0 | 0 | 1000,00 | 14,75 | 20,23 | 1029,56 | False |
| rate-5k-rabbitmq | stream-rate | RabbitMq | 1024 B | 5000/sec | 25000 | 19147 | 5853 | 5786 | 0 | 3829,40 | 2017,37 | 2861,72 | 3199,35 | True |
| rate-5k-redis | stream-rate | Redis | 1024 B | 5000/sec | 25000 | 25000 | 0 | 0 | 0 | 5000,00 | 33,39 | 56,29 | 1103,60 | False |
| rate-10k-rabbitmq | stream-rate | RabbitMq | 1024 B | 10000/sec | 50000 | 9828 | 40172 | 40133 | 0 | 1965,60 | 6071,19 | 7526,01 | 7559,58 | True |
| rate-10k-redis | stream-rate | Redis | 1024 B | 10000/sec | 50000 | 50000 | 0 | 0 | 0 | 10000,00 | 39,23 | 66,79 | 1049,09 | False |
