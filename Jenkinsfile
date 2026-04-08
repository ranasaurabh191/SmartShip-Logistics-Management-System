pipeline {
    agent {
        docker {
            image 'mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2022'
            args '-v //var/run/docker.sock://var/run/docker.sock -v //c/Users/ASUS/OneDrive/Desktop/SmartShip:/workspace'
            reuseNode true
        }
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

        stage('Deploy') {
            steps {
                bat 'docker-compose build --no-cache'
                bat 'docker-compose up -d --force-recreate'
            }
        }
    }

    post {
        success {
            echo '✅ SmartShip deployed!'
        }
        failure {
            bat 'docker-compose logs --tail=100'
        }
    }
}