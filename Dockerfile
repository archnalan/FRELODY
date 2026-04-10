# ============================================================
# FRELODY API – Multi-stage Dockerfile
# Build: mcr.microsoft.com/dotnet/sdk:10.0-alpine  (minimal)
# Runtime: mcr.microsoft.com/dotnet/aspnet:10.0-alpine
# ============================================================

# ─── Stage 1: Restore & Build ────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution and every .csproj so Docker can cache the
# restore layer independently from source changes.
COPY FRELODYAPP.sln ./
COPY FRELODYAPIs/FRELODYAPIs.csproj             FRELODYAPIs/
COPY FRELODYLIB/FRELODYLIB.csproj               FRELODYLIB/
COPY FRELODYSHRD/FRELODYSHRD.csproj             FRELODYSHRD/

# Restore all project dependencies using the solution
RUN dotnet restore FRELODYAPIs/FRELODYAPIs.csproj

# Copy the actual source (respect .dockerignore to keep it lean)
COPY FRELODYAPIs/  FRELODYAPIs/
COPY FRELODYLIB/   FRELODYLIB/
COPY FRELODYSHRD/  FRELODYSHRD/

# Publish:
#   -c Release        → optimised build
#   /p:UseAppHost=false → omit Windows .exe launcher
#   --no-restore      → reuse the already-cached restore
WORKDIR /src/FRELODYAPIs
RUN dotnet publish FRELODYAPIs.csproj \
        -c Release \
        -o /app/publish \
        /p:UseAppHost=false \
        --no-restore

# ─── Stage 2: Runtime Image ──────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install native dependencies required by:
#   • Tesseract OCR NuGet wrapper (charlesw/tesseract 5.2.0)
#     – tesseract-ocr       : native Tesseract 5 engine + libleptonica
#     – tesseract-ocr-data-eng : English LSTM data (fallback; app also ships its own)
#     – gcompat             : glibc shim so the NuGet's P/Invoke resolves on musl
#     – libstdc++           : C++ runtime needed by native libs
#   • .NET globalization (SQL Server / EF Core string ops)
#     – icu-libs            : ICU data & libraries
RUN apk add --no-cache \
        gcompat \
        libstdc++ \
        icu-libs \
        tesseract-ocr \
        tesseract-ocr-data-eng

# Create symlinks so the .NET Tesseract NuGet can find the native libraries
# The NuGet looks for exact names under <app>/x64/
RUN mkdir -p /app/x64 \
    && ln -sf /usr/lib/liblept.so.5    /app/x64/libleptonica-1.82.0.so \
    && ln -sf /usr/lib/libtesseract.so.5 /app/x64/libtesseract50.so

# Enable full ICU globalization (required for SQL Server / EF Core string ops).
# Alpine .NET images default to invariant mode; we override that here.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Application runs on port 8080 (HTTP) inside the container.
ENV ASPNETCORE_URLS=http://+:8080

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Copy tessdata (tessdata_best – most accurate LSTM models) next to the DLLs.
# Only the English traineddata is shipped – add more languages here if needed.
COPY FRELODYAPIs/tessdata/ ./tessdata/

# Create the media upload directory expected by the app
RUN mkdir -p media/images

EXPOSE 8080

ENTRYPOINT ["dotnet", "FRELODYAPIs.dll"]
