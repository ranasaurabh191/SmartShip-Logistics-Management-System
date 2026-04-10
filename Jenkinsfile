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
                    // cd to project directory first (where docker-compose.yml exists)
                    dir('SmartShip Logistics Management System') {
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
                            // Your EXACT manual flow from the right directory
                            bat 'docker compose down --remove-orphans'
                            bat 'docker compose build --no-cache'
                            bat 'docker compose up -d'
                            echo 'Full rebuild complete from project root'
                        } else {
                            echo 'No service changes - skipping'
                        }
                    }
                }
            }
        }

        stage('Status') {
            steps {
                dir('SmartShip Logistics Management System') {
                    bat 'docker compose ps'
                }
            }
        }
    }

    post {
        failure {
            dir('SmartShip Logistics Management System') {
                bat 'docker compose logs --tail=100'
            }
        }
    }
}