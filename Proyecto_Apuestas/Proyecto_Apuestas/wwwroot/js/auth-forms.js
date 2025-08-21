// auth-forms.js
// Manejo unificado de formularios de autenticación para Bet506
// Dependencias: Bootstrap 5, SweetAlert2 (opcional), app-core.js, form-validation.js

(function () {
    'use strict';

    // ========================
    // GESTIÓN DE CONTRASEÑAS - VERSIÓN SIMPLIFICADA
    // ========================
    class PasswordManager {
        constructor() {
            this.init();
        }

        init() {
            this.setupToggleButtons();
            this.setupPasswordStrength();
            this.setupPasswordMatch();
        }

        setupToggleButtons() {
            console.log('[PASSWORD] Setting up toggle buttons - simple version...');
            
            const toggleButtons = [
                { button: 'togglePasswordLogin', input: 'Password', icon: 'togglePasswordLoginIcon' },
                { button: 'togglePasswordRegister', input: 'RegisterPassword', icon: 'togglePasswordRegisterIcon' },
                { button: 'togglePasswordConfirm', input: 'RegisterConfirmPassword', icon: 'togglePasswordConfirmIcon' },
                { button: 'toggleCurrentPassword', input: 'CurrentPassword', icon: 'toggleCurrentPasswordIcon' },
                { button: 'toggleNewPassword', input: 'NewPassword', icon: 'toggleNewPasswordIcon' },
                { button: 'toggleConfirmPassword', input: 'ConfirmPassword', icon: 'toggleConfirmPasswordIcon' },
                { button: 'toggleConfirmPasswordModal', input: 'ConfirmPassword', icon: 'toggleConfirmPasswordModalIcon' }
            ];

            toggleButtons.forEach(({ button, input, icon }) => {
                const toggleBtn = document.getElementById(button);
                const passwordInput = document.getElementById(input);
                const toggleIcon = document.getElementById(icon);

                if (toggleBtn && passwordInput && toggleIcon) {
                    console.log(`[PASSWORD] Setting up button: ${button}`);
                    
                    // Método simple - solo remover onclick anterior
                    toggleBtn.onclick = null;
                    
                    // Agrega event listener directo
                    toggleBtn.addEventListener('click', (e) => {
                        e.preventDefault();
                        console.log(`[PASSWORD] Button clicked: ${button}`);
                        this.togglePasswordVisibility(passwordInput, toggleIcon);
                    });
                    
                    console.log(`[PASSWORD] Button ${button} setup complete`);
                } else {
                    console.warn(`[PASSWORD] Missing elements for ${button}:`, {
                        button: !!toggleBtn,
                        input: !!passwordInput, 
                        icon: !!toggleIcon
                    });
                }
            });
            
            console.log('[PASSWORD] All toggle buttons setup complete');
        }

        togglePasswordVisibility(input, icon) {
            console.log('[PASSWORD] Toggle visibility called', {
                input: input.id,
                currentType: input.type,
                iconClasses: icon.className
            });
            
            const currentType = input.getAttribute('type');
            const newType = currentType === 'password' ? 'text' : 'password';
            const button = icon.closest('button');
            
            // Cambia el tipo de input
            input.setAttribute('type', newType);
            console.log(`[PASSWORD] Changed input type from ${currentType} to ${newType}`);
            
            if (newType === 'text') {
                // Contraseña visible - mostrar ícono de ocultar
                icon.classList.remove('bi-eye');
                icon.classList.add('bi-eye-slash');
                
                if (button) {
                    button.setAttribute('aria-label', 'Ocultar contraseña');
                    button.classList.remove('btn-outline-secondary');
                    button.classList.add('btn-primary');
                    button.title = 'Contraseña visible - Click para ocultar';
                }
                
                console.log('[PASSWORD] Password now visible');
            } else {
                // Contraseña oculta - mostrar ícono de ver
                icon.classList.remove('bi-eye-slash');
                icon.classList.add('bi-eye');
                
                if (button) {
                    button.setAttribute('aria-label', 'Mostrar contraseña');
                    button.classList.remove('btn-primary');
                    button.classList.add('btn-outline-secondary');
                    button.title = 'Contraseña oculta - Click para mostrar';
                }
                
                console.log('[PASSWORD] Password now hidden');
            }
        }

        setupPasswordStrength() {
            this.setupPasswordStrengthForInput('RegisterPassword', 'passwordStrength', 'strengthBar', 'strengthText', 'passwordRequirements');
            this.setupPasswordStrengthForInput('NewPassword', 'passwordStrengthModal', 'strengthBarModal', 'strengthTextModal', 'passwordRequirementsModal');
        }

        setupPasswordStrengthForInput(inputId, strengthId, barId, textId, requirementsId) {
            const passwordInput = document.getElementById(inputId);
            if (passwordInput) {
                passwordInput.addEventListener('input', (e) => {
                    this.updatePasswordStrength(e.target.value, strengthId, barId, textId, requirementsId);
                });
                
                passwordInput.addEventListener('focus', () => {
                    this.showPasswordRequirements(requirementsId);
                });
            }
        }

        updatePasswordStrength(password, strengthId, barId, textId, requirementsId) {
            const strengthBar = document.getElementById(barId);
            const strengthText = document.getElementById(textId);
            const strengthDiv = document.getElementById(strengthId);
            const requirementsDiv = document.getElementById(requirementsId);
            
            if (!strengthBar || !strengthText) return;

            if (strengthDiv) strengthDiv.style.display = 'block';
            if (requirementsDiv) requirementsDiv.style.display = 'block';

            const requirements = this.getPasswordRequirements(password);
            this.updateRequirementsDisplayByPrefix(requirements, requirementsId);
            
            const score = this.calculatePasswordScore(requirements);
            const { width, className, text } = this.getStrengthDisplay(score);

            strengthBar.style.width = width;
            strengthBar.className = `progress-bar ${className}`;
            strengthText.textContent = text;
            strengthText.className = `text-muted ${this.getTextClass(score)}`;
        }

        showPasswordRequirements(requirementsId) {
            const strengthId = requirementsId.replace('Requirements', '');
            const strengthDiv = document.getElementById(strengthId);
            const requirementsDiv = document.getElementById(requirementsId);
            
            if (strengthDiv) strengthDiv.style.display = 'block';
            if (requirementsDiv) requirementsDiv.style.display = 'block';
        }

        updateRequirementsDisplayByPrefix(requirements, requirementsId) {
            let prefix = '';
            if (requirementsId.includes('Modal')) {
                prefix = '-modal';
            }
            
            const reqElements = {
                [`req-length${prefix}`]: requirements.length,
                [`req-uppercase${prefix}`]: requirements.uppercase,
                [`req-lowercase${prefix}`]: requirements.lowercase,
                [`req-number${prefix}`]: requirements.number,
                [`req-special${prefix}`]: requirements.special
            };

            Object.entries(reqElements).forEach(([id, met]) => {
                const element = document.getElementById(id);
                if (element) {
                    const icon = element.querySelector('i');
                    if (met) {
                        icon.className = 'bi bi-check-circle text-success me-1';
                        element.classList.remove('text-danger');
                        element.classList.add('text-success');
                    } else {
                        icon.className = 'bi bi-x-circle text-danger me-1';
                        element.classList.remove('text-success');
                        element.classList.add('text-danger');
                    }
                }
            });
        }

        getPasswordRequirements(password) {
            return {
                length: password.length >= 8,
                uppercase: /[A-Z]/.test(password),
                lowercase: /[a-z]/.test(password),
                number: /\d/.test(password),
                special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
            };
        }

        calculatePasswordScore(requirements) {
            return Object.values(requirements).filter(Boolean).length;
        }

        getStrengthDisplay(score) {
            const displays = {
                0: { width: '0%', className: '', text: 'Ingresa una contraseña' },
                1: { width: '20%', className: 'bg-danger', text: 'Muy débil' },
                2: { width: '40%', className: 'bg-danger', text: 'Débil' },
                3: { width: '60%', className: 'bg-warning', text: 'Regular' },
                4: { width: '80%', className: 'bg-info', text: 'Buena' },
                5: { width: '100%', className: 'bg-success', text: 'Excelente' }
            };
            return displays[score] || displays[0];
        }

        getTextClass(score) {
            if (score <= 1) return 'text-danger';
            if (score <= 2) return 'text-warning';
            if (score <= 3) return 'text-info';
            return 'text-success';
        }

        setupPasswordMatch() {
            const confirmInputs = ['RegisterConfirmPassword', 'ConfirmPassword'];
            const passwordInputs = ['RegisterPassword', 'Password', 'NewPassword'];

            confirmInputs.forEach(confirmId => {
                const confirmInput = document.getElementById(confirmId);
                if (confirmInput) {
                    confirmInput.addEventListener('input', () => {
                        this.checkPasswordMatch();
                        this.checkPasswordMatchModal();
                    });

                    confirmInput.addEventListener('focus', () => {
                        this.showPasswordMatch();
                        this.showPasswordMatchModal();
                    });

                    confirmInput.addEventListener('blur', () => {
                        setTimeout(() => {
                            this.hidePasswordMatch();
                            this.hidePasswordMatchModal();
                        }, 200);
                    });
                }
            });

            passwordInputs.forEach(passwordId => {
                const passwordInput = document.getElementById(passwordId);
                if (passwordInput) {
                    passwordInput.addEventListener('input', () => {
                        this.checkPasswordMatch();
                        this.checkPasswordMatchModal();
                    });
                }
            });
        }

        checkPasswordMatch() {
            const password = this.getPasswordValue();
            const confirmPassword = this.getConfirmPasswordValue();
            const matchDiv = document.getElementById('passwordMatch');
            const matchText = document.getElementById('matchText');

            if (!matchDiv || !matchText) return;

            if (confirmPassword.length === 0) {
                matchText.textContent = 'Las contraseñas deben coincidir';
                matchText.className = 'text-muted';
                return;
            }

            if (password === confirmPassword) {
                matchText.textContent = 'MATCH: Las contraseñas coinciden';
                matchText.className = 'text-success';
            } else {
                matchText.textContent = 'ERROR: Las contraseñas no coinciden';
                matchText.className = 'text-danger';
            }
        }

        checkPasswordMatchModal() {
            const newPassword = document.getElementById('NewPassword')?.value || '';
            const confirmPassword = document.getElementById('ConfirmPassword')?.value || '';
            const matchDiv = document.getElementById('passwordMatchModal');
            const matchText = document.getElementById('matchTextModal');

            if (!matchDiv || !matchText) return;

            if (confirmPassword.length === 0) {
                matchText.textContent = 'Las contraseñas deben coincidir';
                matchText.className = 'text-muted';
                return;
            }

            if (newPassword === confirmPassword) {
                matchText.textContent = 'MATCH: Las contraseñas coinciden';
                matchText.className = 'text-success';
            } else {
                matchText.textContent = 'ERROR: Las contraseñas no coinciden';
                matchText.className = 'text-danger';
            }
        }

        showPasswordMatchModal() {
            const matchDiv = document.getElementById('passwordMatchModal');
            if (matchDiv) matchDiv.style.display = 'block';
        }

        hidePasswordMatchModal() {
            const matchDiv = document.getElementById('passwordMatchModal');
            if (matchDiv) matchDiv.style.display = 'none';
        }

        getPasswordValue() {
            const passwordInputs = ['RegisterPassword', 'Password', 'NewPassword'];
            for (const id of passwordInputs) {
                const input = document.getElementById(id);
                if (input && input.value) {
                    return input.value;
                }
            }
            return '';
        }

        getConfirmPasswordValue() {
            const confirmInputs = ['RegisterConfirmPassword', 'ConfirmPassword'];
            for (const id of confirmInputs) {
                const input = document.getElementById(id);
                if (input) {
                    return input.value;
                }
            }
            return '';
        }

        showPasswordMatch() {
            const matchDiv = document.getElementById('passwordMatch');
            if (matchDiv) matchDiv.style.display = 'block';
        }

        hidePasswordMatch() {
            const matchDiv = document.getElementById('passwordMatch');
            if (matchDiv) matchDiv.style.display = 'none';
        }
    }

    // ========================
    // MANEJADORES DE FORMULARIOS
    // ========================
    class AuthFormManager {
        constructor() {
            this.init();
        }

        init() {
            this.setupLoginForm();
            this.setupRegisterForm();
            this.setupForgotPasswordForm();
            this.setupProfileForm();
            this.setupChangePasswordForm();
        }

        setupLoginForm() {
            const loginForm = document.getElementById('loginForm');
            if (!loginForm) return;

            loginForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                
                const errorDiv = document.getElementById('loginError');
                const submitBtn = loginForm.querySelector('button[type="submit"]');
                
                // Limpia errores anteriores
                if (errorDiv) {
                    errorDiv.classList.add('visually-hidden');
                }

                // Valida formulario
                if (!window.Bet506FormValidation.validateForm('loginForm')) {
                    return;
                }

                const emailOrUsername = loginForm.querySelector('input[name="EmailOrUsername"]').value.trim();
                const password = loginForm.querySelector('input[name="Password"]').value.trim();

                if (!emailOrUsername || !password) {
                    if (errorDiv) {
                        errorDiv.textContent = 'Por favor ingresa email/usuario y contraseña.';
                        errorDiv.classList.remove('visually-hidden');
                    }
                    return;
                }

                window.Bet506.utils.setButtonLoading(submitBtn, 'Ingresando...');

                try {
                    const formData = new FormData(loginForm);
                    const response = await fetch('/Account/Login', {
                        method: 'POST',
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest'
                        },
                        body: formData
                    });

                    const data = await response.json();

                    if (data.success) {
                        await window.Bet506.utils.showSweetAlert('success', '¡Bienvenido!', data.message);
                        window.location.href = data.data?.redirectUrl || '/';
                    } else {
                        if (errorDiv) {
                            errorDiv.textContent = data.message || 'Error en el inicio de sesión.';
                            errorDiv.classList.remove('visually-hidden');
                        }
                    }
                } catch (error) {
                    if (errorDiv) {
                        errorDiv.textContent = 'Error de conexión. Intenta nuevamente.';
                        errorDiv.classList.remove('visually-hidden');
                    }
                    console.error('Login error:', error);
                } finally {
                    window.Bet506.utils.setButtonNormal(submitBtn, '<i class="bi bi-box-arrow-in-right me-1"></i>Ingresar');
                }
            });
        }

        setupRegisterForm() {
            const registerForm = document.getElementById('registerForm');
            if (!registerForm) return;

            registerForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                
                const errorDiv = document.getElementById('registerError');
                const submitBtn = registerForm.querySelector('button[type="submit"]');
                
                // Limpia errores anteriores
                if (errorDiv) {
                    errorDiv.classList.add('visually-hidden');
                }

                // Valida formulario
                if (!window.Bet506FormValidation.validateForm('registerForm')) {
                    return;
                }

                window.Bet506.utils.setButtonLoading(submitBtn, 'Creando cuenta...');

                try {
                    const formData = new FormData(registerForm);
                    const response = await fetch('/Account/Register', {
                        method: 'POST',
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest'
                        },
                        body: formData
                    });

                    const data = await response.json();

                    if (data.success) {
                        await window.Bet506.utils.showSweetAlert('success', '¡Bienvenido!', data.message, 2000);
                        window.location.href = data.data?.redirectUrl || '/';
                    } else {
                        if (errorDiv) {
                            errorDiv.textContent = data.message || 'Error en el registro.';
                            errorDiv.classList.remove('visually-hidden');
                        }
                    }
                } catch (error) {
                    if (errorDiv) {
                        errorDiv.textContent = 'Error de conexión. Intenta nuevamente.';
                        errorDiv.classList.remove('visually-hidden');
                    }
                    console.error('Register error:', error);
                } finally {
                    window.Bet506.utils.setButtonNormal(submitBtn, '<i class="bi bi-person-plus me-1"></i>Crear Cuenta');
                }
            });
        }

        setupForgotPasswordForm() {
            const forgotPasswordForm = document.getElementById('forgotPasswordForm');
            if (!forgotPasswordForm) return;

            forgotPasswordForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                
                const errorDiv = document.getElementById('forgotPasswordError');
                const successDiv = document.getElementById('forgotPasswordSuccess');
                const submitBtn = forgotPasswordForm.querySelector('button[type="submit"]');
                
                // Limpia mensajes anteriores
                if (errorDiv) errorDiv.classList.add('visually-hidden');
                if (successDiv) successDiv.classList.add('visually-hidden');

                // Valida formulario
                if (!window.Bet506FormValidation.validateForm('forgotPasswordForm')) {
                    return;
                }

                window.Bet506.utils.setButtonLoading(submitBtn, 'Enviando...');

                try {
                    const formData = new FormData(forgotPasswordForm);
                    const response = await fetch('/Account/ForgotPassword', {
                        method: 'POST',
                        body: formData
                    });
                    
                    if (response.ok) {
                        if (successDiv) {
                            successDiv.innerHTML = '<i class="bi bi-check-circle me-1"></i>Se han enviado las instrucciones a tu email.';
                            successDiv.classList.remove('visually-hidden');
                        }
                        
                        forgotPasswordForm.reset();
                        window.Bet506FormValidation.resetFormValidation();
                        
                        setTimeout(() => {
                            const modal = bootstrap.Modal.getInstance(document.getElementById('forgotPasswordModal'));
                            if (modal) {
                                modal.hide();
                            }
                        }, 3000);
                    } else {
                        throw new Error('Error en el servidor');
                    }
                } catch (error) {
                    if (errorDiv) {
                        errorDiv.innerHTML = '<i class="bi bi-exclamation-triangle me-1"></i>Error al enviar las instrucciones. Verifica tu email.';
                        errorDiv.classList.remove('visually-hidden');
                    }
                } finally {
                    window.Bet506.utils.setButtonNormal(submitBtn, '<i class="bi bi-send me-1"></i>Enviar Instrucciones');
                }
            });
        }

        setupProfileForm() {
            const editProfileForm = document.getElementById('editProfileForm');
            if (!editProfileForm) return;

            editProfileForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                
                const errorDiv = document.getElementById('editProfileError');
                const successDiv = document.getElementById('editProfileSuccess');
                const submitBtn = editProfileForm.querySelector('button[type="submit"]');
                
                // Limpia mensajes anteriores
                if (errorDiv) errorDiv.classList.add('visually-hidden');
                if (successDiv) successDiv.classList.add('visually-hidden');
                
                // Valida formulario
                if (!window.Bet506FormValidation.validateForm('editProfileForm')) {
                    if (errorDiv) {
                        errorDiv.innerHTML = 'Por favor completa todos los campos requeridos correctamente.';
                        errorDiv.classList.remove('visually-hidden');
                    }
                    return;
                }
                
                window.Bet506.utils.setButtonLoading(submitBtn, 'Guardando...');
                
                try {
                    const formData = new FormData(editProfileForm);
                    const response = await fetch('/Account/EditProfile', {
                        method: 'POST',
                        body: formData
                    });
                    
                    if (response.ok) {
                        if (successDiv) {
                            successDiv.innerHTML = '<i class="bi bi-check-circle me-1"></i>Perfil actualizado exitosamente.';
                            successDiv.classList.remove('visually-hidden');
                        }
                        
                        setTimeout(() => {
                            window.location.reload();
                        }, 2000);
                    } else {
                        throw new Error('Error en el servidor');
                    }
                } catch (error) {
                    if (errorDiv) {
                        errorDiv.innerHTML = '<i class="bi bi-exclamation-triangle me-1"></i>Ocurrió un error. Por favor intenta nuevamente.';
                        errorDiv.classList.remove('visually-hidden');
                    }
                } finally {
                    window.Bet506.utils.setButtonNormal(submitBtn, '<i class="bi bi-check-circle me-1"></i>Guardar Cambios');
                }
            });
        }

        setupChangePasswordForm() {
            const changePasswordForm = document.getElementById('changePasswordForm');
            if (!changePasswordForm) return;

            changePasswordForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                
                const errorDiv = document.getElementById('changePasswordError');
                const successDiv = document.getElementById('changePasswordSuccess');
                const submitBtn = changePasswordForm.querySelector('button[type="submit"]');
                
                // Limpia mensajes anteriores
                if (errorDiv) errorDiv.classList.add('visually-hidden');
                if (successDiv) successDiv.classList.add('visually-hidden');
                
                // Valida formulario
                if (!window.Bet506FormValidation.validateForm('changePasswordForm')) {
                    if (errorDiv) {
                        errorDiv.innerHTML = 'Por favor completa todos los campos correctamente.';
                        errorDiv.classList.remove('visually-hidden');
                    }
                    return;
                }
                
                window.Bet506.utils.setButtonLoading(submitBtn, 'Cambiando...');
                
                try {
                    const formData = new FormData(changePasswordForm);
                    const response = await fetch('/Account/ChangePassword', {
                        method: 'POST',
                        body: formData
                    });
                    
                    if (response.ok) {
                        if (successDiv) {
                            successDiv.innerHTML = '<i class="bi bi-check-circle me-1"></i>Contraseña cambiada exitosamente.';
                            successDiv.classList.remove('visually-hidden');
                        }
                        
                        changePasswordForm.reset();
                        window.Bet506FormValidation.resetFormValidation();
                        
                        setTimeout(() => {
                            const modal = bootstrap.Modal.getInstance(document.getElementById('changePasswordModal'));
                            if (modal) {
                                modal.hide();
                            }
                        }, 2000);
                    } else {
                        throw new Error('Error en el servidor');
                    }
                } catch (error) {
                    if (errorDiv) {
                        errorDiv.innerHTML = '<i class="bi bi-exclamation-triangle me-1"></i>Error al cambiar la contraseña. Verifica tu contraseña actual.';
                        errorDiv.classList.remove('visually-hidden');
                    }
                } finally {
                    window.Bet506.utils.setButtonNormal(submitBtn, '<i class="bi bi-key me-1"></i>Cambiar Contraseña');
                }
            });
        }
    }

    // ========================
    // INICIALIZACIÓN SIMPLIFICADA
    // ========================
    document.addEventListener('DOMContentLoaded', function () {
        console.log('[AUTH] Auth Forms System iniciado - versión simplificada');
        
        // Inicializa gestores
        const passwordManager = new PasswordManager();
        const authFormManager = new AuthFormManager();

        // Exporta para uso global - versión simplificada
        window.Bet506Auth = {
            passwordManager,
            authFormManager,
            // Funciones básicas
            testToggle: (buttonId) => {
                const button = document.getElementById(buttonId);
                if (button) {
                    console.log(`[TEST] Testing button: ${buttonId}`);
                    button.click();
                } else {
                    console.error(`[TEST] Button not found: ${buttonId}`);
                }
            }
        };

        console.log('[AUTH] Auth Forms System inicializado - versión simplificada');
    });

})();