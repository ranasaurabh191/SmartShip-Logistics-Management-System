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

        stage('Selective Rebuild') {
            steps {
                script {
                    // Get changed files
                    def changedFiles = bat(
                        script: 'git diff --name-only HEAD~1 HEAD || echo FIRST_BUILD',
                        returnStdout: true
                    ).trim()

                    def files = changedFiles.split('\n').collect { it.trim() }
                    def serviceChanged = files.any { 
                        it.contains('Services/SmartShip.') || 
                        it.contains('Gateway/SmartShip.Gateway')
                    }

                    if (serviceChanged || !files) {
                        // Full rebuild (your exact manual flow)
                        bat 'docker compose down --remove-orphans'
                        bat 'docker compose build --no-cache'
                        bat 'docker compose up -d'
                        echo 'Full rebuild complete (matches manual docker compose flow)'
                    } else {
                        echo 'No service changes detected - skipping rebuild'
                    }
                }
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