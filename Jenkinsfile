pipeline {
    agent any

    options {
        buildDiscarder(logRotator(numToKeepStr: '5'))
        disableConcurrentBuilds()
        timestamps()
    }

    environment {
        FULL_REBUILD = 'false'
        BUILD_IDENTITY = 'false'
        BUILD_ADMIN = 'false'
        BUILD_SHIPMENT = 'false'
        BUILD_PAYMENT = 'false'
        BUILD_NOTIFICATION = 'false'
        BUILD_GATEWAY = 'false'
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
                    def changedFiles = bat(
                        script: 'git diff --name-only HEAD~1 HEAD || echo "FIRST_BUILD"',
                        returnStdout: true
                    ).trim()

                    echo "Changed files:\n${changedFiles}"

                    if (changedFiles.contains('FIRST_BUILD') || 
                        changedFiles.contains('docker-compose.yml') || 
                        changedFiles.contains('nuget.config') || 
                        changedFiles.contains('Jenkinsfile') || 
                        changedFiles.contains('localfeed/') ||
                        changedFiles.contains('SmartShip.Shared/')) {
                        env.FULL_REBUILD = 'true'
                    }

                    if (changedFiles.contains('SmartShip.IdentityService/')) {
                        env.BUILD_IDENTITY = 'true'
                    }
                    if (changedFiles.contains('SmartShip.AdminService/')) {
                        env.BUILD_ADMIN = 'true'
                    }
                    if (changedFiles.contains('SmartShip.ShipmentService/')) {
                        env.BUILD_SHIPMENT = 'true'
                    }
                    if (changedFiles.contains('SmartShip.PaymentService/')) {
                        env.BUILD_PAYMENT = 'true'
                    }
                    if (changedFiles.contains('SmartShip.NotificationService/')) {
                        env.BUILD_NOTIFICATION = 'true'
                    }
                    if (changedFiles.contains('SmartShip.Gateway/')) {
                        env.BUILD_GATEWAY = 'true'
                    }

                    if (env.FULL_REBUILD == 'true') {
                        env.BUILD_IDENTITY = 'true'
                        env.BUILD_ADMIN = 'true'
                        env.BUILD_SHIPMENT = 'true'
                        env.BUILD_PAYMENT = 'true'
                        env.BUILD_NOTIFICATION = 'true'
                        env.BUILD_GATEWAY = 'true'
                    }

                    echo "FULL_REBUILD=${env.FULL_REBUILD}"
                }
            }
        }

        stage('Restore') {
            steps {
                bat 'dotnet restore "SmartShip Logistics Management System.slnx" --configfile nuget.config'
            }
        }

        stage('Build Identity') {
            when { expression { env.BUILD_IDENTITY == 'true' } }
            steps {
                bat 'dotnet build "Services/SmartShip.IdentityService/SmartShip.IdentityService.csproj" -c Release --no-restore'
                bat 'docker-compose build --no-cache identity-service'
                bat 'docker-compose up -d --force-recreate identity-service'
            }
        }

        stage('Build Admin') {
            when { expression { env.BUILD_ADMIN == 'true' } }
            steps {
                bat 'dotnet build "Services/SmartShip.AdminService/SmartShip.AdminService.csproj" -c Release --no-restore'
                bat 'docker-compose build --no-cache admin-service'
                bat 'docker-compose up -d --force-recreate admin-service'
            }
        }

        stage('Build Shipment') {
            when { expression { env.BUILD_SHIPMENT == 'true' } }
            steps {
                bat 'dotnet build "Services/SmartShip.ShipmentService/SmartShip.ShipmentService.csproj" -c Release --no-restore'
                bat 'docker-compose build --no-cache shipment-service'
                bat 'docker-compose up -d --force-recreate shipment-service'
            }
        }

        stage('Build Payment') {
            when { expression { env.BUILD_PAYMENT == 'true' } }
            steps {
                bat 'dotnet build "Services/SmartShip.PaymentService/SmartShip.PaymentService.csproj" -c Release --no-restore'
                bat 'docker-compose build --no-cache payment-service'
                bat 'docker-compose up -d --force-recreate payment-service'
            }
        }

        stage('Build Notification') {
            when { expression { env.BUILD_NOTIFICATION == 'true' } }
            steps {
                bat 'dotnet build "Services/SmartShip.NotificationService/SmartShip.NotificationService.csproj" -c Release --no-restore'
                bat 'docker-compose build --no-cache notification-service'
                bat 'docker-compose up -d --force-recreate notification-service'
            }
        }

        stage('Build Gateway') {
            when { expression { env.BUILD_GATEWAY == 'true' } }
            steps {
                bat 'dotnet build "Gateway/SmartShip.Gateway/SmartShip.Gateway.csproj" -c Release --no-restore'
                bat 'docker-compose build --no-cache gateway'
                bat 'docker-compose up -d --force-recreate gateway'
            }
        }

        stage('Status') {
            steps {
                bat 'docker-compose ps'
            }
        }
    }

    post {
        success {
            echo '✅ SmartShip selective rebuild complete!'
        }
        failure {
            bat 'docker-compose logs --tail=100'
        }
    }
}