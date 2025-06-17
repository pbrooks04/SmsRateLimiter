# SMS Rate Limiter ASP .NET Core Microservice

The front end to this project can be found here: https://github.com/pbrooks04/SmsRateLimiterUI

## Purpose

This microservice is intended to serve as middleware for an SMS sending program. The goal of this microservice is to restrict request rate limits by two variables, `phoneNumber` and `accountId`. Requests are limited by these values and sending too many requests within a timeframe with a constant `phoneNumber` or `accountId` causes the service to return a 429 response.

## Endpoints

To mock sending an SMS request:
```
POST /api/sms/send
BODY: {
  accountId: string
  phoneNumber: string
  message: string
}
```

To retreive the history log of the most recent 500 entries:
```
GET /api/history
```

## Tests
Tests are included in `SmsRateLimiter.Test`. They ensure that the service is able to send an SMS and that rates are applied based on the `phoneNumber` and `accountId` fields.

## Running the program
It can run locally in development mode with Visual Studio or with docker.

```
docker build -t smsratelimiter .
docker run -it --rm -p 7228:8080 --name smsratelimitercontainer smsratelimiter
```
