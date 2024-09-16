## Run
Standard asp.net project run VS\Rider (or `dotnet run`)

## Test
Use Swagger or http file in the root

## Tests
Run integration tests using VS\Rider console

## Features
- Minimal API
- Typed HttpClient
- Standard resilience handler for retrying, rate limiting, etc
- InMemory output cache
- WebApplicationFactory for integration testing
- Route constraints validation  
- FluentValidation (through filter)
- FluentAssertion

## Enhancements
- Add distributed caching for scalability
- Improve output cache with some external service like Redis
- Add rate limiting to endpoints
- Use NBomber for load testing
- Test Frankfurter API limits
