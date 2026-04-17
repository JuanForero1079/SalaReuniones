namespace SalaReuniones.ViewModels
{
    /// <summary>
    /// ViewModel usado para la vista de administración
    /// con búsqueda, filtros y paginación.
    /// </summary>
    public class UserListViewModel
    {
        /// Lista de usuarios mostrados en la página actual
        public List<UserAdminViewModel> Users { get; set; } = new();

        /// Texto de búsqueda por username
        public string Search { get; set; } = string.Empty;

        /// Filtro de rol seleccionado
        public string RoleFilter { get; set; } = string.Empty;

        /// Página actual
        public int Page { get; set; }

        /// Total de páginas disponibles
        public int TotalPages { get; set; }
    }
}