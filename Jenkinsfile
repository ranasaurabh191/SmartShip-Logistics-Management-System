def FULL_REBUILD = false
def BUILD_IDENTITY = false
def BUILD_ADMIN = false
def BUILD_SHIPMENT = false
def BUILD_PAYMENT = false
def BUILD_NOTIFICATION = false
def BUILD_GATEWAY = false

pipeline {
    agent any

    options {
        buildDiscarder(logRotator(numToKeepStr: '5'))
        disableConcurrentBuilds()
        timestamps()
        skipDefaultCheckout(true)
    }

    environment {
        DOCKER_BUILDKIT = '1'
        COMPOSE_DOCKER_CLI_BUILD = '1'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Detect Changes') {
            steps {
                script {
                    def changedFiles = ''
                    try {
                        changedFiles = bat(
                            script: '@echo off\r\ngit diff --name-only HEAD~1 HEAD',
                            returnStdout: true
                        ).trim()
                    } catch (Exception ex) {
                        changedFiles = 'FIRST_BUILD'
                    }

                    if (!changedFiles?.trim()) {
                        changedFiles = 'NO_CHANGES_DETECTED'
                    }

                    echo "Changed files:\n${changedFiles}"

                    def files = changedFiles
                        .split('\r?\n')
                        .collect { it.trim().replace('\\', '/') }
                        .findAll { it }

                    echo "Normalized files: ${files}"

                    def touches = { prefix ->
                        files.any { f -> f.contains(prefix) }
                    }

                    if (
                        files.contains('FIRST_BUILD') ||
                        touches('docker-compose.yml') ||
                        touches('nuget.config') ||
                        touches('Jenkinsfile') ||
                        touches('BuildingBlocks/SmartShip.Shared/') ||
                        touches('localfeed/')
                    ) {
                        FULL_REBUILD = true
                    }

                    if (touches('Services/SmartShip.IdentityService/'))     BUILD_IDENTITY = true
                    if (touches('Services/SmartShip.AdminService/'))        BUILD_ADMIN = true
                    if (touches('Services/SmartShip.ShipmentService/'))     BUILD_SHIPMENT = true
                    if (touches('Services/SmartShip.PaymentService/'))      BUILD_PAYMENT = true
                    if (touches('Services/SmartShip.NotificationService/')) BUILD_NOTIFICATION = true
                    if (touches('Gateway/SmartShip.Gateway/'))              BUILD_GATEWAY = true

                    if (FULL_REBUILD) {
                        BUILD_IDENTITY = true
                        BUILD_ADMIN = true
                        BUILD_SHIPMENT = true
                        BUILD_PAYMENT = true
                        BUILD_NOTIFICATION = true
                        BUILD_GATEWAY = true
                    }

                    echo """
FULL_REBUILD=${FULL_REBUILD}
BUILD_IDENTITY=${BUILD_IDENTITY}
BUILD_ADMIN=${BUILD_ADMIN}
BUILD_SHIPMENT=${BUILD_SHIPMENT}
BUILD_PAYMENT=${BUILD_PAYMENT}
BUILD_NOTIFICATION=${BUILD_NOTIFICATION}
BUILD_GATEWAY=${BUILD_GATEWAY}
"""
                }
            }
        }
        stage('Prepare Local Feed') {
            steps {
                script {
                    if (!fileExists('localfeed')) {
                        echo 'Creating localfeed directory (empty - Docker will handle restore)'
                        bat 'mkdir localfeed'
                    }
                }
            }
        }
        stage('Restore') {
            when {
                expression { FULL_REBUILD && fileExists('localfeed') }
            }
            steps {
                bat 'dotnet restore "SmartShip Logistics Management System.slnx" --configfile nuget.config'
            }
        }

        stage('Build Identity') {
            when {
                expression { BUILD_IDENTITY }
            }
            steps {
                bat 'docker rm -f smartship-identity 2>nul || exit /b 0'
                bat 'docker compose build identity-service'
                bat 'docker compose up -d --no-deps identity-service'
            }
        }

        stage('Build Admin') {
            when {
                expression { BUILD_ADMIN }
            }
            steps {
                bat 'docker rm -f smartship-admin 2>nul || exit /b 0'
                bat 'docker compose build admin-service'
                bat 'docker compose up -d --no-deps admin-service'
            }
        }

        stage('Build Shipment') {
            when {
                expression { BUILD_SHIPMENT }
            }
            steps {
                bat 'docker rm -f smartship-shipment 2>nul || exit /b 0'
                bat 'docker compose build shipment-service'
                bat 'docker compose up -d --no-deps shipment-service'
            }
        }

        stage('Build Payment') {
            when {
                expression { BUILD_PAYMENT }
            }
            steps {
                bat 'docker rm -f smartship-payment 2>nul || exit /b 0'
                bat 'docker compose build payment-service'
                bat 'docker compose up -d --no-deps payment-service'
            }
        }

        stage('Build Notification') {
            when {
                expression { BUILD_NOTIFICATION }
            }
            steps {
                bat 'docker rm -f smartship-notification 2>nul || exit /b 0'
                bat 'docker compose build notification-service'
                bat 'docker compose up -d --no-deps notification-service'
            }
        }

        stage('Build Tracking') {
            when {
                expression { BUILD_TRACKING }
            }
            steps {
                bat 'docker rm -f smartship-tracking 2>nul || exit /b 0'
                bat 'docker compose build tracking-service'
                bat 'docker compose up -d --no-deps tracking-service'
            }
        }

        stage('Build Gateway') {
            when {
                expression { BUILD_GATEWAY }
            }
            steps {
                bat 'docker rm -f smartship-gateway 2>nul || exit /b 0'
                bat 'docker compose build api-gateway'
                bat 'docker compose up -d --no-deps api-gateway'
            }
        }

        stage('Status') {
            steps {
                bat 'docker compose ps'
            }
        }
    }

    post {
        success {
            echo 'SmartShip Selective CI/CD complete'
        }
        failure {
            bat 'docker compose logs --tail=100'
        }
    }
}