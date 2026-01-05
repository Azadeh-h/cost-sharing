# .NET MAUI Build Environment Container
# This container is for building the app, NOT for running it
# The output will be native installers (.exe, .apk, .dmg, .ipa)

FROM mcr.microsoft.com/dotnet/sdk:9.0

# Install MAUI workload
RUN dotnet workload install maui

# Install Android SDK dependencies (for building .apk)
RUN apt-get update && apt-get install -y \
    openjdk-17-jdk \
    unzip \
    && rm -rf /var/lib/apt/lists/*

# Set Java home for Android builds
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64

# Install Android command line tools
ENV ANDROID_SDK_ROOT=/usr/local/android-sdk
RUN mkdir -p ${ANDROID_SDK_ROOT}/cmdline-tools && \
    cd ${ANDROID_SDK_ROOT}/cmdline-tools && \
    wget https://dl.google.com/android/repository/commandlinetools-linux-9477386_latest.zip && \
    unzip commandlinetools-linux-9477386_latest.zip && \
    rm commandlinetools-linux-9477386_latest.zip && \
    mv cmdline-tools latest

ENV PATH="${ANDROID_SDK_ROOT}/cmdline-tools/latest/bin:${PATH}"

# Accept Android SDK licenses
RUN yes | sdkmanager --licenses

# Install Android build tools and platform
RUN sdkmanager "platform-tools" "platforms;android-34" "build-tools;34.0.0"

# Set working directory
WORKDIR /app

# Copy solution and project files
COPY CostSharingApp.sln ./
COPY src/CostSharingApp/*.csproj ./src/CostSharingApp/
COPY src/CostSharing.Core/*.csproj ./src/CostSharing.Core/
COPY tests/CostSharingApp.Tests/*.csproj ./tests/CostSharingApp.Tests/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build command examples (uncomment as needed):
# Build for Android: dotnet build src/CostSharingApp/CostSharingApp.csproj -f net9.0-android -c Release
# Build for Windows: dotnet build src/CostSharingApp/CostSharingApp.csproj -f net9.0-windows10.0.19041.0 -c Release
# Publish Android APK: dotnet publish src/CostSharingApp/CostSharingApp.csproj -f net9.0-android -c Release -o ./output/android

# Default command - keep container running for interactive builds
CMD ["bash"]
