pipeline {
    agent {
        docker {
            image 'mcr.microsoft.com/dotnet/sdk:10.0'
            args '-v /var/run/docker.sock:/var/run/docker.sock'
        }
    }

    options {
        buildDiscarder(logRotator(numToKeepStr: '5'))
        disableConcurrentBuilds()
        timestamps()
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore') {
            steps {
                bat 'dotnet restore "SmartShip Logistics Management System.slnx" --configfile nuget.config'
            }
        }

        stage('Build') {
            steps {
                bat 'dotnet build "SmartShip Logistics Management System.slnx" -c Release --no-restore'
            }
        }

        stage('Docker Deploy') {
            steps {
                bat 'docker-compose build --no-cache'
                bat 'docker-compose up -d --force-recreate'
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
            echo '✅ SmartShip deployed successfully!'
        }
        failure {
            bat 'docker-compose logs --tail=100'
        }
    }
}