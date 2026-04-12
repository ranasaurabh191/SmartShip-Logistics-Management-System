def FULL_REBUILD    = false
def BUILD_IDENTITY  = false
def BUILD_ADMIN     = false
def BUILD_SHIPMENT  = false
def BUILD_PAYMENT   = false
def BUILD_NOTIFICATION = false
def BUILD_TRACKING  = false
def BUILD_GATEWAY   = false
def PROJECT_DIR = 'C:\\Users\\ASUS\\OneDrive\\Desktop\\SmartShip Logistics Management System'

pipeline {
    agent any

    options {
        buildDiscarder(logRotator(numToKeepStr: '5'))
        disableConcurrentBuilds()
        timestamps()
        skipDefaultCheckout(true)
    }

    environment {
        DOCKER_BUILDKIT          = '1'
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

                    def touches = { prefix ->
                        files.any { f -> f.contains(prefix) }
                    }

                    if (
                        files.contains('FIRST_BUILD')           ||
                        touches('docker-compose.yml')           ||
                        touches('nuget.config')                 ||
                        touches('Shared/SmartShip.Shared/')     ||
                        touches('BuildingBlocks/SmartShip.Shared/')
                    ) {
                        FULL_REBUILD = true
                    }

                    if (touches('Services/SmartShip.IdentityService/'))     BUILD_IDENTITY     = true
                    if (touches('Services/SmartShip.AdminService/'))        BUILD_ADMIN        = true
                    if (touches('Services/SmartShip.ShipmentService/'))     BUILD_SHIPMENT     = true
                    if (touches('Services/SmartShip.PaymentService/'))      BUILD_PAYMENT      = true
                    if (touches('Services/SmartShip.NotificationService/')) BUILD_NOTIFICATION = true
                    if (touches('Services/SmartShip.TrackingService/'))     BUILD_TRACKING     = true
                    if (touches('Gateway/SmartShip.Gateway/'))              BUILD_GATEWAY      = true

                    if (FULL_REBUILD) {
                        BUILD_IDENTITY     = true
                        BUILD_ADMIN        = true
                        BUILD_SHIPMENT     = true
                        BUILD_PAYMENT      = true
                        BUILD_NOTIFICATION = true
                        BUILD_TRACKING     = true
                        BUILD_GATEWAY      = true
                    }

                    echo """
                    FULL_REBUILD=${FULL_REBUILD}
                    BUILD_IDENTITY=${BUILD_IDENTITY}
                    BUILD_ADMIN=${BUILD_ADMIN}
                    BUILD_SHIPMENT=${BUILD_SHIPMENT}
                    BUILD_PAYMENT=${BUILD_PAYMENT}
                    BUILD_NOTIFICATION=${BUILD_NOTIFICATION}
                    BUILD_TRACKING=${BUILD_TRACKING}
                    BUILD_GATEWAY=${BUILD_GATEWAY}
                    """
                }
            }
        }

        stage('Full Rebuild') {
            when {
                expression { FULL_REBUILD }
            }
            steps {
                dir(PROJECT_DIR){
                bat 'docker compose down --remove-orphans'
                bat 'docker compose build --no-cache'
                bat 'docker compose up -d'
                }
            }
        }

        stage('Build Identity') {
            when {
                expression { !FULL_REBUILD && BUILD_IDENTITY }
            }
            steps {
                dir(PROJECT_DIR){
                bat 'docker compose stop identity-service'
                bat 'docker compose rm -f identity-service'
                bat 'docker compose build --no-cache identity-service'
                bat 'docker compose up -d --no-deps identity-service'
                }
            }
        }

        stage('Build Admin') {
            when {
                expression { !FULL_REBUILD && BUILD_ADMIN }
            }
            steps {
                dir(PROJECT_DIR){
                bat 'docker compose stop admin-service'
                bat 'docker compose rm -f admin-service'
                bat 'docker compose build --no-cache admin-service'
                bat 'docker compose up -d --no-deps admin-service'
                }
            }
        }

        stage('Build Shipment') {
            when {
                expression { !FULL_REBUILD && BUILD_SHIPMENT }
            }
            steps {
                dir(PROJECT_DIR){
                bat 'docker compose stop shipment-service'
                bat 'docker compose rm -f shipment-service'
                bat 'docker compose build --no-cache shipment-service'
                bat 'docker compose up -d --no-deps shipment-service'
                }
            }
        }

        stage('Build Payment') {
            when {
                expression { !FULL_REBUILD && BUILD_PAYMENT }
            }
            steps {
                dir(PROJECT_DIR){
                bat 'docker compose stop payment-service'
                bat 'docker compose rm -f payment-service'
                bat 'docker compose build --no-cache payment-service'
                bat 'docker compose up -d --no-deps payment-service'
                }
            }
        }

        stage('Build Notification') {
            when {
                expression { !FULL_REBUILD && BUILD_NOTIFICATION }
            }
            steps {
                dir(PROJECT_DIR){
                bat 'docker compose stop notification-service'
                bat 'docker compose rm -f notification-service'
                bat 'docker compose build --no-cache notification-service'
                bat 'docker compose up -d --no-deps notification-service'
                }
            }
        }

        stage('Build Tracking') {
            when {
                expression { !FULL_REBUILD && BUILD_TRACKING }
            }
            steps {
                dir(PROJECT_DIR){
                bat 'docker compose stop tracking-service'
                bat 'docker compose rm -f tracking-service'
                bat 'docker compose build --no-cache tracking-service'
                bat 'docker compose up -d --no-deps tracking-service'
                }
            }
        }

        stage('Build Gateway') {
            when {
                expression { !FULL_REBUILD && BUILD_GATEWAY }
            }
            steps {
                dir(PROJECT_DIR){
                bat 'docker compose stop api-gateway'
                bat 'docker compose rm -f api-gateway'
                bat 'docker compose build --no-cache api-gateway'
                bat 'docker compose up -d --no-deps api-gateway'
                }
            }
        }

        stage('Status') {
            steps {
                dir(PROJECT_DIR){
                bat 'docker compose ps'
                bat 'docker image prune -f'
                }
            }
        }

    }

        post {
        success {
            echo 'SmartShip selective CI/CD complete'
            dir(PROJECT_DIR) {                   
                bat 'docker image prune -f'
            }
        }
        failure {
            dir(PROJECT_DIR) {                   
                bat 'docker compose logs --tail=100'
            }
        }
    }

}