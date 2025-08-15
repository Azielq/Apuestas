// form-validation.js
// Sistema unificado de validación de formularios para Bet506
// Dependencias: Bootstrap 5

(function () {
    'use strict';

    // ========================
    // ESTADO DE VALIDACIÓN
    // ========================
    const fieldInteractionState = {};
    const validationRules = {
        costaRicanPhone: [
            /^\+?506[2678]\d{7}$/,           // +506 + 8 dígitos
            /^[2678]\d{7}$/,                 // Sin código de país (8 dígitos)
            /^\+?506\s?[2678]\d{3}[\-\s]?\d{4}$/, // Con separadores
            /^[2678]\d{3}[\-\s]?\d{4}$/     // Formato local con separadores
        ],
        email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
        username: /^[a-zA-Z0-9_]+$/,
        name: /^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$/,
        validCountries: ['Costa Rica', 'Guatemala', 'Honduras', 'El Salvador', 'Nicaragua', 'Panamá']
    };

    // ========================
    // FUNCIONES DE VALIDACIÓN CORE
    // ========================
    function setFieldValid(input) {
        input.classList.remove('is-invalid');
        input.classList.add('is-valid');
    }

    function setFieldInvalid(input, message) {
        input.classList.remove('is-valid');
        input.classList.add('is-invalid');
        const feedback = input.parentNode.querySelector('.invalid-feedback') || 
                        input.closest('.form-check')?.querySelector('.invalid-feedback');
        if (feedback && message) feedback.textContent = message;
    }

    function clearFieldValidation(input) {
        input.classList.remove('is-valid', 'is-invalid');
    }

    function hasInteracted(fieldId) {
        return fieldInteractionState[fieldId] || false;
    }

    function markAsInteracted(fieldId) {
        fieldInteractionState[fieldId] = true;
    }

    // ========================
    // VALIDADORES ESPECÍFICOS
    // ========================
    const validators = {
        name: function(input) {
            const value = input.value.trim();
            const isRequired = input.hasAttribute('required');
            const fieldId = input.id;
            
            if (!value && !hasInteracted(fieldId)) {
                clearFieldValidation(input);
                return true;
            }
            
            if (isRequired && !value) {
                setFieldInvalid(input, 'Este campo es requerido');
                return false;
            }
            
            if (value && !validationRules.name.test(value)) {
                setFieldInvalid(input, 'Solo se permiten letras y espacios');
                return false;
            }
            
            if (value && value.length > parseInt(input.getAttribute('maxlength') || '80')) {
                setFieldInvalid(input, 'Demasiado largo');
                return false;
            }
            
            if (value || !isRequired) setFieldValid(input);
            return true;
        },

        username: function(input) {
            const value = input.value.trim();
            const fieldId = input.id;
            
            if (!value && !hasInteracted(fieldId)) {
                clearFieldValidation(input);
                return true;
            }
            
            if (!value) {
                setFieldInvalid(input, 'El nombre de usuario es requerido');
                return false;
            }
            
            if (value.length < 3) {
                setFieldInvalid(input, 'Mínimo 3 caracteres');
                return false;
            }
            
            if (value.length > 45) {
                setFieldInvalid(input, 'Máximo 45 caracteres');
                return false;
            }
            
            if (!validationRules.username.test(value)) {
                setFieldInvalid(input, 'Solo letras, números y guión bajo');
                return false;
            }
            
            setFieldValid(input);
            return true;
        },

        email: function(input) {
            const value = input.value.trim();
            const fieldId = input.id;
            
            if (!value && !hasInteracted(fieldId)) {
                clearFieldValidation(input);
                return true;
            }
            
            if (!value) {
                setFieldInvalid(input, 'El email es requerido');
                return false;
            }
            
            if (value.length > 45) {
                setFieldInvalid(input, 'Máximo 45 caracteres');
                return false;
            }
            
            if (!validationRules.email.test(value)) {
                setFieldInvalid(input, 'Formato de email inválido');
                return false;
            }
            
            setFieldValid(input);
            return true;
        },

        emailOrUsername: function(input) {
            const value = input.value.trim();
            const fieldId = input.id;
            
            if (!value && !hasInteracted(fieldId)) {
                clearFieldValidation(input);
                return true;
            }
            
            if (!value) {
                setFieldInvalid(input, 'Email o usuario es requerido');
                return false;
            }
            
            // Validación básica - solo verifica que no esté vacío
            setFieldValid(input);
            return true;
        },

        password: function(input) {
            const value = input.value;
            const fieldId = input.id;
            
            if (!value && !hasInteracted(fieldId)) {
                clearFieldValidation(input);
                return true;
            }
            
            if (!value) {
                setFieldInvalid(input, 'La contraseña es requerida');
                return false;
            }
            
            // Para contraseñas de registro (más estricta)
            if (fieldId.includes('Register') || fieldId === 'NewPassword') {
                const requirements = {
                    length: value.length >= 8,
                    uppercase: /[A-Z]/.test(value),
                    lowercase: /[a-z]/.test(value),
                    number: /\d/.test(value),
                    special: /[^a-zA-Z0-9]/.test(value)
                };
                const score = Object.values(requirements).filter(Boolean).length;
                
                if (score < 4) {
                    setFieldInvalid(input, 'La contraseña no cumple los requisitos mínimos');
                    return false;
                }
            } else {
                // Para login (menos estricta)
                if (value.length < 6) {
                    setFieldInvalid(input, 'La contraseña debe tener al menos 6 caracteres');
                    return false;
                }
            }
            
            setFieldValid(input);
            return true;
        },

        passwordMatch: function(passwordInputId, confirmInputId) {
            const passwordInput = document.getElementById(passwordInputId);
            const confirmInput = document.getElementById(confirmInputId);
            
            if (!passwordInput || !confirmInput) return true;
            
            const password = passwordInput.value;
            const confirmValue = confirmInput.value;
            const fieldId = confirmInput.id;
            
            if (!confirmValue && !hasInteracted(fieldId)) {
                clearFieldValidation(confirmInput);
                return true;
            }
            
            if (!confirmValue) {
                setFieldInvalid(confirmInput, 'Confirma tu contraseña');
                return false;
            }
            
            if (password !== confirmValue) {
                setFieldInvalid(confirmInput, 'Las contraseñas no coinciden');
                return false;
            }
            
            setFieldValid(confirmInput);
            return true;
        },

        country: function(input) {
            const value = input.value;
            const fieldId = input.id;
            
            if (!value && !hasInteracted(fieldId)) {
                clearFieldValidation(input);
                return true;
            }
            
            if (!value) {
                setFieldInvalid(input, 'Debes seleccionar un país');
                return false;
            }
            
            if (!validationRules.validCountries.includes(value)) {
                setFieldInvalid(input, 'País no válido');
                return false;
            }
            
            setFieldValid(input);
            return true;
        },

        terms: function(input) {
            const fieldId = input.id;
            
            if (!hasInteracted(fieldId)) {
                clearFieldValidation(input);
                return true;
            }
            
            if (!input.checked) {
                setFieldInvalid(input, 'Debes aceptar los términos y condiciones');
                return false;
            }
            
            setFieldValid(input);
            return true;
        },

        costaRicanPhone: function(input) {
            const phone = input.value.trim();
            const fieldId = input.id;
            
            // Campo opcional - solo valida si hay contenido o se ha interactuado
            if (phone.length === 0 && !hasInteracted(fieldId)) {
                clearFieldValidation(input);
                return true;
            }
            
            if (phone.length === 0) {
                clearFieldValidation(input);
                return true;
            }
            
            const isValid = validationRules.costaRicanPhone.some(pattern => pattern.test(phone));
            
            if (isValid) {
                input.classList.add('is-valid');
                input.classList.remove('is-invalid');
            } else {
                input.classList.add('is-invalid');
                input.classList.remove('is-valid');
            }
            
            return isValid;
        }
    };

    // ========================
    // CONFIGURACIÓN DE CAMPOS
    // ========================
    function setupFieldValidation(fieldId, validatorName) {
        const field = document.getElementById(fieldId);
        if (!field || !validators[validatorName]) return;

        field.addEventListener('input', function () {
            if (this.value.length > 0) {
                markAsInteracted(fieldId);
            }
            if (hasInteracted(fieldId)) {
                validators[validatorName](this);
            }
        });

        field.addEventListener('blur', function () {
            markAsInteracted(fieldId);
            validators[validatorName](this);
        });
    }

    function setupPasswordMatchValidation(passwordId, confirmId) {
        const passwordInput = document.getElementById(passwordId);
        const confirmInput = document.getElementById(confirmId);
        
        if (!passwordInput || !confirmInput) return;

        const validateMatch = () => validators.passwordMatch(passwordId, confirmId);

        passwordInput.addEventListener('input', function () {
            if (this.value.length > 0) {
                markAsInteracted(this.id);
            }
            if (hasInteracted(this.id)) {
                validators.password(this);
                if (confirmInput.value) validateMatch();
            }
        });

        passwordInput.addEventListener('blur', function () {
            markAsInteracted(this.id);
            validators.password(this);
        });

        confirmInput.addEventListener('input', function () {
            if (this.value.length > 0) {
                markAsInteracted(this.id);
            }
            validateMatch();
        });

        confirmInput.addEventListener('blur', function () {
            markAsInteracted(this.id);
            validateMatch();
        });
    }

    // ========================
    // INICIALIZACIÓN DE FORMULARIOS
    // ========================
    function initRegisterForm() {
        // Campos básicos
        setupFieldValidation('RegisterFirstName', 'name');
        setupFieldValidation('RegisterPrimerApellido', 'name');
        setupFieldValidation('RegisterSegundoApellido', 'name');
        setupFieldValidation('RegisterUserName', 'username');
        setupFieldValidation('RegisterEmail', 'email');
        setupFieldValidation('RegisterCountry', 'country');
        
        // Teléfono (validación especial)
        const phoneInput = document.getElementById('RegisterPhoneNumber');
        if (phoneInput) {
            phoneInput.addEventListener('input', function () {
                if (this.value.length > 0) {
                    markAsInteracted(this.id);
                }
                validators.costaRicanPhone(this);
            });
            phoneInput.addEventListener('blur', function () {
                markAsInteracted(this.id);
                validators.costaRicanPhone(this);
            });
        }
        
        // Contraseñas
        setupPasswordMatchValidation('RegisterPassword', 'RegisterConfirmPassword');
        
        // Términos
        const termsCheckbox = document.getElementById('RegisterTerms');
        if (termsCheckbox) {
            termsCheckbox.addEventListener('change', function () {
                markAsInteracted(this.id);
                validators.terms(this);
            });
        }
    }

    function initLoginForm() {
        // CORREGIDO: Ahora usa el sistema de interacción consistente
        setupFieldValidation('EmailOrUsername', 'emailOrUsername');
        setupFieldValidation('Password', 'password');
    }

    function initForgotPasswordForm() {
        setupFieldValidation('ForgotEmail', 'email');
    }

    function initProfileForm() {
        // Campos de perfil
        setupFieldValidation('EditFirstName', 'name');
        setupFieldValidation('EditPrimerApellido', 'name');
        setupFieldValidation('EditSegundoApellido', 'name');
        setupFieldValidation('EditCountry', 'country');
        
        // Teléfono de perfil
        const profilePhoneInput = document.getElementById('EditPhoneNumber');
        if (profilePhoneInput) {
            profilePhoneInput.addEventListener('input', function () {
                if (this.value.length > 0) {
                    markAsInteracted(this.id);
                }
                validators.costaRicanPhone(this);
            });
            profilePhoneInput.addEventListener('blur', function () {
                markAsInteracted(this.id);
                validators.costaRicanPhone(this);
            });
        }
    }

    function initChangePasswordForm() {
        setupPasswordMatchValidation('NewPassword', 'ConfirmPassword');
        
        // Campo de contraseña actual usando el sistema consistente
        setupFieldValidation('CurrentPassword', 'password');
    }

    // ========================
    // GESTIÓN DE RESET
    // ========================
    function resetFormValidation() {
        // Limpia estado de interacción
        Object.keys(fieldInteractionState).forEach(key => {
            delete fieldInteractionState[key];
        });
        
        // Limpia clases de validación de todos los campos
        const fields = document.querySelectorAll('.form-control, .form-check-input');
        fields.forEach(field => {
            clearFieldValidation(field);
        });

        // Fuerza ocultamiento de todos los mensajes .invalid-feedback
        const invalidFeedbacks = document.querySelectorAll('.invalid-feedback');
        invalidFeedbacks.forEach(feedback => {
            feedback.style.display = 'none';
        });

        // Limpia campos que puedan tener la clase was-validated de Bootstrap
        const forms = document.querySelectorAll('form');
        forms.forEach(form => {
            form.classList.remove('was-validated');
        });
        
        console.log('🧹 Form validation reset completed');
    }

    function setupModalHandlers() {
        ['loginModal', 'registerModal', 'forgotPasswordModal', 'editProfileModal', 'changePasswordModal'].forEach(modalId => {
            const modal = document.getElementById(modalId);
            if (modal) {
                modal.addEventListener('hidden.bs.modal', () => {
                    // Pequeño delay para asegurar que el modal esté completamente cerrado
                    setTimeout(resetFormValidation, 100);
                });
                modal.addEventListener('show.bs.modal', () => {
                    // Reset inmediato al abrir
                    resetFormValidation();
                });
                modal.addEventListener('shown.bs.modal', () => {
                    // Reset adicional después de que el modal esté completamente visible
                    setTimeout(() => {
                        resetFormValidation();
                        // Fuerza ocultamiento adicional usando la función global
                        if (window.Bet506?.utils?.forceHideValidationMessages) {
                            window.Bet506.utils.forceHideValidationMessages();
                        }
                    }, 50);
                });
            }
        });
    }

    // ========================
    // VALIDACIONES PÚBLICAS
    // ========================
    function validateForm(formId) {
        let isValid = true;
        const form = document.getElementById(formId);
        if (!form) return false;

        // Marca todos los campos como interactuados para mostrar errores
        const inputs = form.querySelectorAll('input, select, textarea');
        inputs.forEach(input => {
            if (input.id) {
                markAsInteracted(input.id);
            }
        });

        // Valida según el tipo de formulario
        switch (formId) {
            case 'registerForm':
                isValid = validateRegisterForm();
                break;
            case 'loginForm':
                isValid = validateLoginForm();
                break;
            case 'forgotPasswordForm':
                isValid = validateForgotPasswordForm();
                break;
            case 'editProfileForm':
                isValid = validateProfileForm();
                break;
            case 'changePasswordForm':
                isValid = validateChangePasswordForm();
                break;
        }

        return isValid;
    }

    function validateRegisterForm() {
        let isValid = true;
        
        // Valida campos individuales
        isValid &= validators.name(document.getElementById('RegisterFirstName'));
        isValid &= validators.name(document.getElementById('RegisterPrimerApellido'));
        isValid &= validators.username(document.getElementById('RegisterUserName'));
        isValid &= validators.email(document.getElementById('RegisterEmail'));
        isValid &= validators.country(document.getElementById('RegisterCountry'));
        isValid &= validators.password(document.getElementById('RegisterPassword'));
        isValid &= validators.passwordMatch('RegisterPassword', 'RegisterConfirmPassword');
        isValid &= validators.terms(document.getElementById('RegisterTerms'));
        
        return Boolean(isValid);
    }

    function validateLoginForm() {
        let isValid = true;
        const emailField = document.getElementById('EmailOrUsername');
        const passwordField = document.getElementById('Password');
        
        if (emailField) {
            isValid &= validators.emailOrUsername(emailField);
        }
        
        if (passwordField) {
            isValid &= validators.password(passwordField);
        }
        
        return Boolean(isValid);
    }

    function validateForgotPasswordForm() {
        const emailField = document.getElementById('ForgotEmail');
        return emailField ? validators.email(emailField) : false;
    }

    function validateProfileForm() {
        let isValid = true;
        
        isValid &= validators.name(document.getElementById('EditFirstName'));
        isValid &= validators.name(document.getElementById('EditPrimerApellido'));
        isValid &= validators.country(document.getElementById('EditCountry'));
        
        return Boolean(isValid);
    }

    function validateChangePasswordForm() {
        let isValid = true;
        
        const currentField = document.getElementById('CurrentPassword');
        const newField = document.getElementById('NewPassword');
        
        if (currentField) {
            isValid &= validators.password(currentField);
        }
        
        if (newField) {
            isValid &= validators.password(newField);
        }
        
        isValid &= validators.passwordMatch('NewPassword', 'ConfirmPassword');
        
        return Boolean(isValid);
    }

    // ========================
    // INICIALIZACIÓN
    // ========================
    document.addEventListener('DOMContentLoaded', function () {
        console.log('🔍 Form Validation System iniciado');
        
        // Configura manejadores de modal
        setupModalHandlers();
        
        // Inicializa formularios
        initRegisterForm();
        initLoginForm();
        initForgotPasswordForm();
        initProfileForm();
        initChangePasswordForm();

        // Exporta funciones públicas
        window.Bet506FormValidation = {
            validateForm,
            resetFormValidation,
            validators,
            setupFieldValidation,
            setupPasswordMatchValidation,
            clearFieldValidation,
            fieldInteractionState, // Para debugging
            // Función de debugging
            debugValidation: function() {
                console.group('🔍 Form Validation Debug Info');
                console.log('Field Interaction State:', fieldInteractionState);
                
                const invalidFields = document.querySelectorAll('.is-invalid');
                const validFields = document.querySelectorAll('.is-valid');
                const invalidFeedbacks = document.querySelectorAll('.invalid-feedback');
                
                console.log('Invalid Fields:', invalidFields);
                console.log('Valid Fields:', validFields);
                console.log('Invalid Feedbacks:', invalidFeedbacks);
                
                // Muestra feedbacks visibles
                const visibleFeedbacks = Array.from(invalidFeedbacks).filter(el => {
                    const style = window.getComputedStyle(el);
                    return style.display !== 'none' && style.visibility !== 'hidden';
                });
                console.log('Visible Invalid Feedbacks:', visibleFeedbacks);
                
                console.groupEnd();
            },
            // Función para limpiar todo forzadamente
            forceCleanAll: function() {
                console.log('🧨 Force cleaning all validation states...');
                resetFormValidation();
                if (window.Bet506?.utils?.forceHideValidationMessages) {
                    window.Bet506.utils.forceHideValidationMessages();
                }
                // Limpia formularios que puedan tener was-validated
                document.querySelectorAll('form').forEach(form => {
                    form.classList.remove('was-validated', 'needs-validation');
                    form.noValidate = true;
                });
                console.log('✅ Force clean completed');
            }
        };

        console.log('✅ Form Validation System inicializado');
    });

})();