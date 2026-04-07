pipeline {
    agent any

    environment {
        ASPNETCORE_ENVIRONMENT = 'DockerJenkins'
        COMPOSE_PROJECT_NAME   = 'smartship'
    }

    stages {
        stage('Checkout') {
            steps { checkout scm }
        }

        stage('Restore & Build') {
            steps {
                sh 'dotnet restore SmartShip.sln'
                sh 'dotnet build SmartShip.sln -c Release --no-restore'
            }
        }

        stage('Run Tests') {
            steps {
                sh 'dotnet test SmartShip.sln --no-build -c Release'
            }
        }

        stage('Docker Build') {
            steps {
                sh 'docker-compose build --parallel'
            }
        }

        stage('Docker Deploy') {
            steps {
                sh 'docker-compose down --remove-orphans'
                sh 'docker-compose up -d --force-recreate'
            }
        }

        stage('Health Check') {
            steps {
                sleep(time: 30, unit: 'SECONDS')
                sh 'curl -f http://localhost:5000/health || exit 1'
            }
        }
    }

    post {
        success { echo '✅ SmartShip deployed successfully!' }
        failure {
            echo '❌ Pipeline failed.'
            sh 'docker-compose logs --tail=50'
        }
    }
}