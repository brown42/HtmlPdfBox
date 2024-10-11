FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

USER root

# some parts borrowed from https://github.com/puppeteer/puppeteer/blob/main/docker/Dockerfile

# Configure default locale (important for chrome-headless-shell).
ENV LANG en_US.UTF-8
ENV DBUS_SESSION_BUS_ADDRESS autolaunch:
ENV NVM_DIR "/root/.nvm"
ENV NVM_DIR /usr/local/nvm
ENV NODE_VERSION 20.18.0

# Install latest chrome dev package and fonts to support major charsets (Chinese, Japanese, Arabic, Hebrew, Thai and a few others)
# Note: this installs the necessary libs to make the bundled version of Chrome that Puppeteer
# installs, work.
RUN apt-get update \
    && apt-get install -y --no-install-recommends fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-khmeros \
    fonts-kacst fonts-freefont-ttf dbus dbus-x11 dumb-init curl 


RUN mkdir -p /usr/local/nvm
RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.0/install.sh | bash
RUN /bin/bash -c "source $NVM_DIR/nvm.sh && nvm install $NODE_VERSION && nvm use --delete-prefix $NODE_VERSION"

ENV NODE_PATH $NVM_DIR/versions/node/v$NODE_VERSION/bin
ENV PATH $NODE_PATH:$PATH

RUN npm i @puppeteer/browsers \
    && npx @puppeteer/browsers install chrome@128.0.6613.119 --install-deps 

ENV PUPPETEER_EXECUTABLE_PATH /app/chrome/linux-128.0.6613.119/chrome-linux64/chrome

ENTRYPOINT ["/usr/bin/dumb-init", "--"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HtmlPdfBox/HtmlPdfBox.csproj", "HtmlPdfBox/"]
RUN dotnet restore "HtmlPdfBox/HtmlPdfBox.csproj"
COPY . .
WORKDIR "/src/HtmlPdfBox"
RUN dotnet build "HtmlPdfBox.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HtmlPdfBox.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
CMD ["dotnet", "HtmlPdfBox.dll", "ChromePath=/usr/bin/google-chrome", "OutputPath=/home/headless/output_cache", "InternalUrl=http://localhost:8080"]