# This Makefile is silent by default, use `make -s` to override
.SILENT:

# Phony targets to avoid file name conflict
.PHONY: help sonar

# Include environment variables from .env file
-include .env
export $(shell sed 's/=.*//' .env)

# Display help information about this Makefile.
help:
	@echo "Available targets:"
	@echo "  sonar: Run SonarQube analysis"

# Target for running SonarQube analysis
sonar:
	# Echo a message
	echo "Running SonarQube analysis"
	
	# Begin SonarQube analysis
	# If this command fails, the make command will stop
	dotnet sonarscanner begin /k:"${SONAR_PROJECT_NAME}" /d:sonar.host.url="${SONAR_HOST_URL}"  /d:sonar.login="${SONAR_LOGIN}" || exit 1
	
	# Build the project
	# If this command fails, the make command will stop
	dotnet build || exit 1
	
	# End SonarQube analysis
	# If this command fails, the make command will stop
	dotnet sonarscanner end /d:sonar.login="${SONAR_LOGIN}" || exit 1

