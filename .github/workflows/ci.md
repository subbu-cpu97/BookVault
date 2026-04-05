# CI pipeline — runs on every push and pull request to main
# Interview answer: "What is CI/CD?"
# CI (Continuous Integration): automatically build and test every code change.
# CD (Continuous Delivery/Deployment): automatically deploy passing builds.
# CI catches bugs before they merge. CD eliminates manual deployment steps.
# Every professional team has CI — it's table stakes, not optional.

name: BookVault CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

# Cancel in-progress runs when a new push arrives on the same branch
# Saves GitHub Actions minutes — no point testing old code
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

env:
  DOTNET_VERSION: '10.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  # ── Job 1: Build ──────────────────────────────────────────────────
  build:
    name: Build
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET ${{ env.DOTNET_VERSION }}
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      # Cache NuGet packages — avoids re-downloading on every run
      # Interview answer: "Why cache in CI?"
      # dotnet restore downloads packages from NuGet.org on every run.
      # With 50 packages that's 30+ seconds. Caching reduces it to <2s.
      # Cache key includes the lock file hash — invalidates when deps change.
      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Restore dependencies
        run: dotnet restore

      # --no-restore: already restored above, skip redundant step
      # -warnaserror: treat warnings as errors — no warnings allowed in CI
      # Interview answer: "What does -warnaserror do and why use it?"
      # Warnings are future errors. Teams that ignore warnings accumulate
      # hundreds of them — then a real problem hides among the noise.
      # Treating warnings as errors from day 1 keeps the codebase clean.
      - name: Build
        run: |
          dotnet build \
            --no-restore \
            --configuration Release \
            -warnaserror

      - name: Check code formatting
        run: |
          dotnet format --verify-no-changes --no-restore \
            --severity warn

      # Upload build artifacts so later jobs don't rebuild
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: build-output
          path: '**/bin/Release/**'
          retention-days: 1

  # ── Job 2: Unit Tests ─────────────────────────────────────────────
  unit-tests:
    name: Unit Tests
    runs-on: ubuntu-latest
    needs: build   # only runs if build passes

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Restore dependencies
        run: dotnet restore

      # Run unit tests with code coverage collection
      # Interview answer: "What is code coverage?"
      # Coverage measures which lines of code are executed by tests.
      # 80% coverage means 80% of lines were hit by at least one test.
      # It's a hygiene metric — not a quality metric. 100% coverage with
      # poor assertions is worse than 70% coverage with strong assertions.
      # We collect it to track trends, not to hit an arbitrary number.
      - name: Run unit tests
        run: |
          dotnet test tests/BookVault.UnitTests \
            --no-restore \
            --configuration Release \
            --logger "trx;LogFileName=unit-test-results.trx" \
            --collect:"XPlat Code Coverage" \
            --results-directory ./test-results

      # Publish test results as a GitHub check — shows pass/fail per test
      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()   # run even if tests fail
        with:
          name: Unit Test Results
          path: test-results/**/*.trx
          reporter: dotnet-trx
          fail-on-error: true

      # Upload coverage report for the coverage job
      - name: Upload coverage report
        uses: actions/upload-artifact@v4
        with:
          name: unit-coverage
          path: test-results/**/coverage.cobertura.xml

  # ── Job 3: Architecture Tests ─────────────────────────────────────
  architecture-tests:
    name: Architecture Tests
    runs-on: ubuntu-latest
    needs: build

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Restore dependencies
        run: dotnet restore

      # Architecture tests run fast — no Docker, no DB — just reflection
      # Interview answer: "Why run architecture tests in their own CI job?"
      # Separation makes failures clear. If arch tests fail you immediately
      # know it's an architecture violation, not a logic bug or DB issue.
      # Parallel jobs also make the pipeline faster overall.
      - name: Run architecture tests
        run: |
          dotnet test tests/BookVault.ArchitectureTests \
            --no-restore \
            --configuration Release \
            --logger "trx;LogFileName=arch-test-results.trx" \
            --results-directory ./test-results

      - name: Publish architecture test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Architecture Test Results
          path: test-results/**/*.trx
          reporter: dotnet-trx
          fail-on-error: true

  # ── Job 4: Integration Tests ──────────────────────────────────────
  integration-tests:
    name: Integration Tests
    runs-on: ubuntu-latest
    needs: build

    # Service containers — GitHub Actions spins up real Docker services
    # Interview answer: "What are GitHub Actions service containers?"
    # They're Docker containers that run alongside your job.
    # Your integration tests connect to this real Postgres instance —
    # same as TestContainers but managed by GitHub Actions infrastructure.
    # TestContainers also works in CI — it starts Docker inside the runner.
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_USER: bookvault
          POSTGRES_PASSWORD: bookvault_pass
          POSTGRES_DB: bookvault_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Restore dependencies
        run: dotnet restore

      - name: Run integration tests
        env:
          # Override connection string for CI environment
          ConnectionStrings__DefaultConnection: >-
            Host=localhost;Port=5432;
            Database=bookvault_test;
            Username=bookvault;
            Password=bookvault_pass
        run: |
          dotnet test tests/BookVault.IntegrationTests \
            --no-restore \
            --configuration Release \
            --logger "trx;LogFileName=integration-test-results.trx" \
            --collect:"XPlat Code Coverage" \
            --results-directory ./test-results

      - name: Publish integration test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Integration Test Results
          path: test-results/**/*.trx
          reporter: dotnet-trx
          fail-on-error: true

      - name: Upload integration coverage
        uses: actions/upload-artifact@v4
        with:
          name: integration-coverage
          path: test-results/**/coverage.cobertura.xml

  # ── Job 5: Code Coverage Report ───────────────────────────────────
  coverage:
    name: Code Coverage
    runs-on: ubuntu-latest
    needs: [ unit-tests, integration-tests ]

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Download unit coverage
        uses: actions/download-artifact@v4
        with:
          name: unit-coverage
          path: coverage/unit

      - name: Download integration coverage
        uses: actions/download-artifact@v4
        with:
          name: integration-coverage
          path: coverage/integration

      # ReportGenerator merges multiple coverage files into one HTML report
      - name: Install ReportGenerator
        run: |
          dotnet tool install --global dotnet-reportgenerator-globaltool

      - name: Generate coverage report
        run: |
          reportgenerator \
            -reports:"coverage/**/*.xml" \
            -targetdir:"coverage/report" \
            -reporttypes:"Html;Cobertura;MarkdownSummaryGithub" \
            -verbosity:Warning

      # Post coverage summary as a PR comment
      - name: Post coverage summary
        if: github.event_name == 'pull_request'
        uses: marocchino/sticky-pull-request-comment@v2
        with:
          path: coverage/report/SummaryGithub.md

      - name: Upload HTML coverage report
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coverage/report/
          retention-days: 7

  # ── Job 6: Security scan ──────────────────────────────────────────
  security:
    name: Security Scan
    runs-on: ubuntu-latest
    needs: build

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore

      # Check NuGet packages for known vulnerabilities
      # Interview answer: "What does dotnet list package --vulnerable do?"
      # NuGet maintains a vulnerability database. This command checks every
      # package in your solution against it. A package with a CVE (Common
      # Vulnerability and Exposure) fails the build, forcing an upgrade.
      # Run this in CI so vulnerabilities are caught before they ship.
      - name: Check for vulnerable packages
        run: |
          dotnet list package --vulnerable --include-transitive \
            2>&1 | tee vulnerable-packages.txt
          if grep -q "has the following vulnerable packages" vulnerable-packages.txt; then
            echo "Vulnerable packages found!"
            cat vulnerable-packages.txt
            exit 1
          fi

  # ── Job 7: Docker build validation ───────────────────────────────
  docker-build:
    name: Docker Build
    runs-on: ubuntu-latest
    needs: build

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      # Validate the Dockerfile builds successfully without pushing
      # Interview answer: "Why build the Docker image in CI without pushing?"
      # Dockerfile syntax errors and missing files only show up at build time.
      # Building in CI catches them on every PR — not when deploying to prod.
      - name: Build Docker image
        run: |
          docker build \
            -f docker/api/Dockerfile \
            -t bookvault-api:ci-${{ github.sha }} \
            .

      - name: Scan Docker image for vulnerabilities
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: bookvault-api:ci-${{ github.sha }}
          format: table
          exit-code: '0'   # warn but don't fail on first setup
          severity: CRITICAL,HIGH
