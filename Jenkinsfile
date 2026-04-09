pipeline {
    agent any

    options {
        buildDiscarder(logRotator(numToKeepStr: '5'))
        disableConcurrentBuilds()
        timestamps()
        skipDefaultCheckout(true)
    }

    environment {
        FULL_REBUILD       = 'false'
        BUILD_IDENTITY     = 'false'
        BUILD_ADMIN        = 'false'
        BUILD_SHIPMENT     = 'false'
        BUILD_PAYMENT      = 'false'
        BUILD_NOTIFICATION = 'false'
        BUILD_GATEWAY      = 'false'
        DOCKER_BUILDKIT    = '1'
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

                    def files = changedFiles.readLines()

                    def touches = { keyword ->
                        files.any { it.replace('\\', '/').contains(keyword) }
                    }

                    if (
                        changedFiles.contains('FIRST_BUILD') ||
                        touches('docker-compose.yml') ||
                        touches('nuget.config') ||
                        touches('Jenkinsfile') ||
                        touches('BuildingBlocks/SmartShip.Shared') ||
                        touches('localfeed/')
                    ) {
                        env.FULL_REBUILD = 'true'
                    }

                    if (touches('Services/SmartShip.IdentityService/'))     env.BUILD_IDENTITY = 'true'
                    if (touches('Services/SmartShip.AdminService/'))        env.BUILD_ADMIN = 'true'
                    if (touches('Services/SmartShip.ShipmentService/'))     env.BUILD_SHIPMENT = 'true'
                    if (touches('Services/SmartShip.PaymentService/'))      env.BUILD_PAYMENT = 'true'
                    if (touches('Services/SmartShip.NotificationService/')) env.BUILD_NOTIFICATION = 'true'
                    if (touches('Gateway/SmartShip.Gateway/'))              env.BUILD_GATEWAY = 'true'

                    if (env.FULL_REBUILD == 'true') {
                        env.BUILD_IDENTITY     = 'true'
                        env.BUILD_ADMIN        = 'true'
                        env.BUILD_SHIPMENT     = 'true'
                        env.BUILD_PAYMENT      = 'true'
                        env.BUILD_NOTIFICATION = 'true'
                        env.BUILD_GATEWAY      = 'true'
                    }

                    echo """
FULL_REBUILD=${env.FULL_REBUILD}
BUILD_IDENTITY=${env.BUILD_IDENTITY}
BUILD_ADMIN=${env.BUILD_ADMIN}
BUILD_SHIPMENT=${env.BUILD_SHIPMENT}
BUILD_PAYMENT=${env.BUILD_PAYMENT}
BUILD_NOTIFICATION=${env.BUILD_NOTIFICATION}
BUILD_GATEWAY=${env.BUILD_GATEWAY}
"""
                }
            }
        }

        stage('Restore') {
            when {
                expression { env.FULL_REBUILD == 'true' }
            }
            steps {
                bat 'dotnet restore "SmartShip Logistics Management System.slnx" --configfile nuget.config'
            }
        }

        stage('Build Identity') {
            when {
                beforeAgent true
                expression { env.BUILD_IDENTITY == 'true' }
            }
            steps {
                bat 'docker compose build identity-service'
                bat 'docker compose up -d --no-deps identity-service'
            }
        }

        stage('Build Admin') {
            when {
                beforeAgent true
                expression { env.BUILD_ADMIN == 'true' }
            }
            steps {
                bat 'docker compose build admin-service'
                bat 'docker compose up -d --no-deps admin-service'
            }
        }

        stage('Build Shipment') {
            when {
                beforeAgent true
                expression { env.BUILD_SHIPMENT == 'true' }
            }
            steps {
                bat 'docker compose build shipment-service'
                bat 'docker compose up -d --no-deps shipment-service'
            }
        }

        stage('Build Payment') {
            when {
                beforeAgent true
                expression { env.BUILD_PAYMENT == 'true' }
            }
            steps {
                bat 'docker compose build payment-service'
                bat 'docker compose up -d --no-deps payment-service'
            }
        }

        stage('Build Notification') {
            when {
                beforeAgent true
                expression { env.BUILD_NOTIFICATION == 'true' }
            }
            steps {
                bat 'docker compose build notification-service'
                bat 'docker compose up -d --no-deps notification-service'
            }
        }

        stage('Build Gateway') {
            when {
                beforeAgent true
                expression { env.BUILD_GATEWAY == 'true' }
            }
            steps {
                bat 'docker compose build gateway'
                bat 'docker compose up -d --no-deps gateway'
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
            echo 'SmartShip selective CI/CD complete'
        }
        failure {
            bat 'docker compose logs --tail=100'
        }
    }
}