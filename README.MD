A very minimal api to render html (strings, and urls) to pdfs using headless chrome.

- The api supports a basic api key check.
- You're responsible for checking html for safety.
- You're responsible for checking urls for safety.

**Imporant** 

The api key check is bypassed when rendering custom html strings. During this time
if an attacker knew the url, and an ID for a render request they may be able to
capture the temporarily cached html and its contents.

## Known Issues

In the current configuration only a single instance of headless chrome can be started. Concurrent requests can be made to this api, but they will block until the
current rendering process finishes.

Plans are to support parallel rendering.

## Configuration

The following settings can be configured in appsettings.json, environment variables,
or passed through the command line.

See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#default-application-configuration-sources for more information.

### Settings

| key         | value                                               |
|-------------|-----------------------------------------------------|
| ChromePath  | Path to chrome executable                           |
| OutputPath  | Path to pdf output cache                            |
| InternalUrl | Local URL for HtmlPdfBox                            |
| ApiKey      | API key to access service, pass in X-Api-Key header |

## Docker

**Docker forces most settings, the only thing you should set is the ApiKey**

You can do this by configuring an environment variable named either:

`ASPNETCORE_ApiKey` or `DOTNET_ApiKey`

## Docker Compose

`docker compose up --detach`

## Building Docker

- Either build the dockerfile or run it locally using the included docker-compose file

## Building Solution

run `dotnet build` from the solution directory.

## Usage

- All requests should include an `X-Api-Key` header, the default value is `PleaseConfigureThis`

There are two endpoints:

POST `/render/html`
```json
{ "html": "<html><body>some html</body></html>" }
```

POST `/render/url`
```json
{ "url": "https://www.google.com" }
```