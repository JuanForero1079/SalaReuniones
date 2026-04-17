namespace SalaReuniones.ViewModels
{
    /// <summary>
    /// ViewModel usado en el panel de administración
    /// para mostrar información de usuarios.
    /// </summary>
    public class UserAdminViewModel
    {
        /// <summary>
        /// ID del usuario en Identity
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de usuario
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Rol asignado
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Estado del usuario (Activo / Inhabilitado)
        /// </summary>
        public string Estado { get; set; } = string.Empty;
    }
}