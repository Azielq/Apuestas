document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('loginForm');
    const loginError = document.getElementById('loginError');

    loginForm.addEventListener('submit', async function (e) {
        e.preventDefault();
        loginError.textContent = '';
        loginError.classList.add('visually-hidden');

        // Obtén valores del form
        const email = loginForm.querySelector('input[name="Email"]').value.trim();
        const password = loginForm.querySelector('input[name="Password"]').value.trim();
        const rememberMe = loginForm.querySelector('input[name="RememberMe"]').checked;

        // Validación
        if (!email || !password) {
            loginError.textContent = 'Por favor ingresa email y contraseña.';
            loginError.classList.remove('visually-hidden');
            return;
        }

        try {
            const response = await fetch('/account/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({ Email: email, Password: password, RememberMe: rememberMe })
            });

            if (response.ok) {
                const data = await response.json();

                if (data.success) {
                    Swal.fire({
                        icon: 'success',
                        title: '¡Bienvenido!',
                        text: data.message,
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        const loginModal = bootstrap.Modal.getInstance(document.getElementById('loginModal'));
                        loginModal.hide();
                        location.reload();
                    });
                } else {
                    loginError.textContent = data.message || 'Error desconocido.';
                    loginError.classList.remove('visually-hidden');
                }
            } else {
                const data = await response.json();
                loginError.textContent = data.message || 'Error en el inicio de sesión.';
                loginError.classList.remove('visually-hidden');
            }
        } catch (error) {
            loginError.textContent = 'Error de conexión. Intenta nuevamente.';
            loginError.classList.remove('visually-hidden');
            console.error('Login error:', error);
        }
    });

    function getAntiForgeryToken() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : '';
    }
});
﻿function abrirLogin() {
    Swal.fire({
        title: 'Iniciar sesión',
        html:
            `<input type="email" id="email" class="swal2-input" placeholder="Correo electrónico">
              <input type="password" id="password" class="swal2-input" placeholder="Contraseña">`,
        confirmButtonText: 'Ingresar',
        focusConfirm: false,
        preConfirm: () => {

        }
    });
}
