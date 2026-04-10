pipeline {
    agent any

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

        stage('Full Rebuild') {
            steps {
                bat 'docker compose down --remove-orphans'
                bat 'docker compose build --no-cache'
                bat 'docker compose up -d'
            }
        }

        stage('Status') {
            steps {
                bat 'docker compose ps'
            }
        }
    }

    post {
        failure {
            bat 'docker compose logs --tail=100'
        }
    }
}