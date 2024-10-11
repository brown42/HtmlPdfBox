A very minimal api to render html (strings, and urls) as pdfs using headless chrome.

## Configuration

The following settings can be configured in appsettings.json, environment variables,
or passed through the command line.

See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#default-application-configuration-sources for more information.

### Settings

| key         | value                                                      |
|-------------|------------------------------------------------------------|
| ChromePath  | Path to chrome executable                                  |
| OutputPath  | Path to pdf output cache                                   |
| InternalUrl | Local URL for HtmlPdfBox                                   |
| ApiKey      | API key to access service, pass in X-Api-Key header        |
| MaxWorkers  | Maximum number of chrome worker instances to allow at once |

## Docker

Build and install the docker image, or use the provided docker-compose file.

To use the container configure two environment variables:
- DOTNET_ApiKey
- DOTNET_MaxWorkers

## Building

run `dotnet build` from the solution directory.

## Usage

- All requests should include an `X-Api-Key` header, the default value is `PleaseConfigureThis`

There are two endpoints, both return the pdf directly.

POST `/render/html`
```json
{ 
  "html": "<html><body>some html</body></html>", 
  "baseUrl": "https://foo" // optional, only necessary if your html has relative urls
}
```

POST `/render/url`
```json
{ 
  "url": "https://www.google.com"
}
```