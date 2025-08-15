// app-core.js
// Funcionalidades centrales de la aplicación Bet506
// Dependencias: Bootstrap 5, SweetAlert2 (opcional)

(function () {
    'use strict';

    // ========================
    // GESTIÓN DE CONEXIONES
    // ========================
    class ConnectionManager {
        constructor() {
            this.isOnline = navigator.onLine;
            this.connectionStatus = 'unknown';
            this.reconnectAttempts = 0;
            this.maxReconnectAttempts = 5;
            this.reconnectDelay = 2000;
            this.healthCheckInterval = null;
            
            this.init();
        }
        
        init() {
            this.setupEventListeners();
            this.startHealthCheck();
            this.showConnectionStatus();
        }
        
        setupEventListeners() {
            window.addEventListener('online', () => {
                this.isOnline = true;
                this.checkConnection();
                this.hideOfflineNotification();
            });
            
            window.addEventListener('offline', () => {
                this.isOnline = false;
                this.connectionStatus = 'offline';
                this.showOfflineNotification();
            });
            
            // Intercepta errores fetch globalmente
            const originalFetch = window.fetch;
            window.fetch = async (...args) => {
                try {
                    const response = await originalFetch(...args);
                    if (!response.ok && (response.status >= 500 || response.status === 0)) {
                        this.handleConnectionError();
                    }
                    return response;
                } catch (error) {
                    this.handleConnectionError();
                    throw error;
                }
            };
        }
        
        async checkConnection() {
            try {
                const controller = new AbortController();
                const timeoutId = setTimeout(() => controller.abort(), 5000);
                
                // Usa endpoint que ya existe y soporta HEAD
                const response = await fetch('/health/status', {
                    method: 'HEAD',
                    cache: 'no-cache',
                    signal: controller.signal
                });
                
                clearTimeout(timeoutId);
                
                if (response.ok) {
                    this.connectionStatus = 'online';
                    this.reconnectAttempts = 0;
                    this.hideConnectionError();
                    return true;
                } else {
                    this.connectionStatus = 'error';
                    return false;
                }
            } catch (error) {
                this.connectionStatus = 'error';
                return false;
            }
        }
        
        async handleConnectionError() {
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                this.reconnectAttempts++;
                console.log(`Intento de reconexión ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);
                
                setTimeout(() => {
                    this.checkConnection().then(isConnected => {
                        if (!isConnected) {
                            this.handleConnectionError();
                        }
                    });
                }, this.reconnectDelay * this.reconnectAttempts);
            } else {
                this.showConnectionError();
            }
        }
        
        startHealthCheck() {
            // Comentar para deshabilitar health checks automáticos
            // this.healthCheckInterval = setInterval(() => {
            //     if (this.isOnline) {
            //         this.checkConnection();
            //     }
            // }, 30000);
            
            console.log('[CONNECTION] Health checks disabled');
        }
        
        showConnectionStatus() {
            if (!document.getElementById('connection-status')) {
                const statusIndicator = document.createElement('div');
                statusIndicator.id = 'connection-status';
                statusIndicator.className = 'position-fixed';
                statusIndicator.style.cssText = `
                    top: 20px;
                    right: 20px;
                    z-index: 9999;
                    padding: 8px 12px;
                    border-radius: 20px;
                    font-size: 12px;
                    font-weight: 500;
                    display: none;
                    opacity: 0.9;
                `;
                document.body.appendChild(statusIndicator);
            }
        }
        
        updateConnectionStatus(status, message) {
            const indicator = document.getElementById('connection-status');
            if (indicator) {
                indicator.textContent = message;
                indicator.style.display = 'block';
                
                switch (status) {
                    case 'online':
                        indicator.className = 'position-fixed bg-success text-white';
                        setTimeout(() => {
                            indicator.style.display = 'none';
                        }, 3000);
                        break;
                    case 'offline':
                        indicator.className = 'position-fixed bg-danger text-white';
                        break;
                    case 'reconnecting':
                        indicator.className = 'position-fixed bg-warning text-dark';
                        break;
                    default:
                        indicator.style.display = 'none';
                }
            }
        }
        
        showOfflineNotification() {
            this.updateConnectionStatus('offline', 'Sin conexion a internet');
        }
        
        hideOfflineNotification() {
            this.updateConnectionStatus('online', 'Conexion restaurada');
        }
        
        showConnectionError() {
            this.showToast('Error de conexión', 'No se pudo conectar con el servidor. Intenta nuevamente.', 'error');
        }
        
        hideConnectionError() {
            const errorToasts = document.querySelectorAll('.toast-error');
            errorToasts.forEach(toast => toast.remove());
        }
        
        showToast(title, message, type = 'info') {
            const toast = document.createElement('div');
            toast.className = `toast-${type} position-fixed`;
            toast.style.cssText = `
                top: 80px;
                right: 20px;
                z-index: 10000;
                min-width: 300px;
                background: ${type === 'error' ? '#dc3545' : type === 'warning' ? '#ffc107' : '#28a745'};
                color: ${type === 'warning' ? '#000' : '#fff'};
                padding: 15px;
                border-radius: 8px;
                box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                animation: slideInFromRight 0.3s ease-out;
            `;
            
            toast.innerHTML = `
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <strong>${title}</strong>
                        <div class="mt-1">${message}</div>
                    </div>
                    <button class="btn-close btn-close-${type === 'warning' ? 'dark' : 'white'} ms-3" onclick="this.parentElement.parentElement.remove()"></button>
                </div>
            `;
            
            document.body.appendChild(toast);
            
            setTimeout(() => {
                if (toast.parentElement) {
                    toast.remove();
                }
            }, 5000);
        }
        
        async testConnection() {
            this.updateConnectionStatus('reconnecting', 'Verificando conexion...');
            const isConnected = await this.checkConnection();
            
            if (isConnected) {
                this.showToast('Conexión exitosa', 'La conexión con el servidor se ha restablecido.', 'success');
            } else {
                this.showToast('Error de conexión', 'No se pudo conectar con el servidor.', 'error');
            }
            
            return isConnected;
        }
    }

    // ========================
    // GESTIÓN DE MODALES
    // ========================
    class ModalManager {
        constructor() {
            this.activeModal = null;
        }

        switchModal(fromModalId, toModalId, delay = 300) {
            const fromModal = fromModalId ? bootstrap.Modal.getInstance(document.getElementById(fromModalId)) : null;
            const toModalElement = document.getElementById(toModalId);
            
            if (fromModal) {
                fromModal.hide();
            }
            
            setTimeout(() => {
                if (toModalElement) {
                    const toModal = new bootstrap.Modal(toModalElement);
                    toModal.show();
                    this.activeModal = toModalId;
                }
            }, delay);
        }

        // Funciones específicas para compatibilidad
        switchToLogin() {
            const forgotModal = bootstrap.Modal.getInstance(document.getElementById('forgotPasswordModal'));
            const registerModal = bootstrap.Modal.getInstance(document.getElementById('registerModal'));
            
            if (forgotModal) forgotModal.hide();
            if (registerModal) registerModal.hide();
            
            setTimeout(() => {
                const loginModal = new bootstrap.Modal(document.getElementById('loginModal'));
                loginModal.show();
                this.activeModal = 'loginModal';
            }, 300);
        }

        switchToRegister() {
            const loginModal = bootstrap.Modal.getInstance(document.getElementById('loginModal'));
            const forgotModal = bootstrap.Modal.getInstance(document.getElementById('forgotPasswordModal'));
            
            if (loginModal) loginModal.hide();
            if (forgotModal) forgotModal.hide();
            
            setTimeout(() => {
                const registerModal = new bootstrap.Modal(document.getElementById('registerModal'));
                registerModal.show();
                this.activeModal = 'registerModal';
            }, 300);
        }

        switchToForgotPassword() {
            const loginModal = bootstrap.Modal.getInstance(document.getElementById('loginModal'));
            const registerModal = bootstrap.Modal.getInstance(document.getElementById('registerModal'));
            
            if (loginModal) loginModal.hide();
            if (registerModal) registerModal.hide();
            
            setTimeout(() => {
                const forgotModal = new bootstrap.Modal(document.getElementById('forgotPasswordModal'));
                forgotModal.show();
                this.activeModal = 'forgotPasswordModal';
            }, 300);
        }
    }

    // ========================
    // UTILIDADES GENERALES
    // ========================
    class AppUtilities {
        static initAOS() {
            if (window.AOS && typeof AOS.init === 'function') {
                AOS.init({
                    duration: 800,
                    easing: 'ease-in-out',
                    once: true
                });
            }
        }

        static initTooltips() {
            if (window.bootstrap && bootstrap.Tooltip) {
                const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
                tooltipTriggerList.map(function (tooltipTriggerEl) {
                    return new bootstrap.Tooltip(tooltipTriggerEl);
                });
            }
        }

        static showAlert(elementId, message, type = 'danger') {
            const element = document.getElementById(elementId);
            if (element) {
                element.innerHTML = message;
                element.className = `alert alert-${type}`;
                element.classList.remove('visually-hidden');
            }
        }

        static hideAlert(elementId) {
            const element = document.getElementById(elementId);
            if (element) {
                element.classList.add('visually-hidden');
            }
        }

        static setButtonLoading(button, text) {
            if (button) {
                button.disabled = true;
                button.innerHTML = `<i class="bi bi-hourglass-split me-1"></i>${text}`;
            }
        }

        static setButtonNormal(button, text) {
            if (button) {
                button.disabled = false;
                button.innerHTML = text;
            }
        }

        static getAntiForgeryToken() {
            const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
            return tokenElement ? tokenElement.value : '';
        }

        static showSweetAlert(icon, title, text, timer = 1500) {
            if (typeof Swal !== 'undefined') {
                return Swal.fire({
                    icon: icon,
                    title: title,
                    text: text,
                    timer: timer,
                    showConfirmButton: false
                });
            }
            return Promise.resolve();
        }

        // Función para forzar el ocultamiento de mensajes de validación
        static forceHideValidationMessages() {
            const invalidFeedbacks = document.querySelectorAll('.invalid-feedback');
            invalidFeedbacks.forEach(feedback => {
                feedback.style.display = 'none';
            });
            
            const fields = document.querySelectorAll('.is-invalid, .is-valid');
            fields.forEach(field => {
                field.classList.remove('is-invalid', 'is-valid');
            });
            
            console.log('[VALIDATION] Messages force-hidden');
        }

        static ensureBootstrapIcons() {
            return new Promise((resolve) => {
                if (document.querySelector('link[href*="bootstrap-icons"]')) {
                    console.log('[ICONS] Bootstrap Icons ya estan cargados');
                    resolve(true);
                } else {
                    const iconStylesheet = document.createElement('link');
                    iconStylesheet.rel = 'stylesheet';
                    iconStylesheet.href = 'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.5.0/font/bootstrap-icons.css';
                    iconStylesheet.onload = () => {
                        console.log('[ICONS] Bootstrap Icons cargados exitosamente');
                        resolve(true);
                    };
                    iconStylesheet.onerror = () => {
                        console.error('[ICONS] Error al cargar Bootstrap Icons');
                        resolve(false);
                    };
                    document.head.appendChild(iconStylesheet);
                }
            });
        }

        static verifyPasswordIcons() {
            const eyeTest = document.createElement('i');
            eyeTest.className = 'bi bi-eye';
            const eyeSlashTest = document.createElement('i');
            eyeSlashTest.className = 'bi bi-eye-slash';
            
            // Agrega temporalmente al DOM para probar
            document.body.appendChild(eyeTest);
            document.body.appendChild(eyeSlashTest);
            
            const eyeStyle = window.getComputedStyle(eyeTest);
            const eyeSlashStyle = window.getComputedStyle(eyeSlashTest);
            
            const eyeContent = eyeStyle.content;
            const eyeSlashContent = eyeSlashStyle.content;
            
            document.body.removeChild(eyeTest);
            document.body.removeChild(eyeSlashTest);
            
            console.log('[ICONS] Icon verification:', {
                eyeContent,
                eyeSlashContent,
                eyeWorking: eyeContent !== 'none' && eyeContent !== '',
                eyeSlashWorking: eyeSlashContent !== 'none' && eyeSlashContent !== ''
            });
            
            return {
                eyeWorking: eyeContent !== 'none' && eyeContent !== '',
                eyeSlashWorking: eyeSlashContent !== 'none' && eyeSlashContent !== ''
            };
        }
    }

    // ========================
    // INICIALIZACIÓN
    // ========================
    document.addEventListener('DOMContentLoaded', function () {
        console.log('[APP] Bet506 App Core iniciado');
        
        // Verifica Bootstrap Icons antes de continuar
        AppUtilities.ensureBootstrapIcons().then((iconsLoaded) => {
            if (iconsLoaded) {
                const iconVerification = AppUtilities.verifyPasswordIcons();
                if (!iconVerification.eyeWorking || !iconVerification.eyeSlashWorking) {
                    console.warn('[APP] Algunos iconos de contraseña pueden no funcionar correctamente');
                }
            }
            
            // Inicializa componentes
            window.Bet506 = {
                connectionManager: new ConnectionManager(),
                modalManager: new ModalManager(),
                utils: AppUtilities
            };

            // Inicializa utilidades
            AppUtilities.initAOS();
            AppUtilities.initTooltips();

            // Fuerza ocultamiento inicial de mensajes de validación
            AppUtilities.forceHideValidationMessages();

            // Funciones globales para compatibilidad con HTML existente
            window.switchToLogin = () => window.Bet506.modalManager.switchToLogin();
            window.switchToRegister = () => window.Bet506.modalManager.switchToRegister();
            window.switchToForgotPassword = () => window.Bet506.modalManager.switchToForgotPassword();

            console.log('[APP] Bet506 App Core inicializado exitosamente');
        });
    });

})();