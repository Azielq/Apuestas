function abrirLogin() {
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