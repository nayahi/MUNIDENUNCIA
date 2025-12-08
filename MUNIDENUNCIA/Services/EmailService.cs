namespace MUNIDENUNCIA.Services
{
    //En produccion se podria usar SendGrid, AWS SES, o el servidor SMTP de la institución/empresa.
    //La interfaz permite este cambio sin modificar el código que usa el servicio,
    //demostrando el principio de inversión de dependencias.
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(
            string email,
            string subject,
            string message)
        {
            _logger.LogInformation(
                "Email simulado a {Email} con asunto: {Subject}",
                email,
                subject);

            Console.WriteLine($"Email simulado enviado a: {email}");
            Console.WriteLine($"Asunto: {subject}");
            Console.WriteLine($"Mensaje: {message}");

            return Task.CompletedTask;
        }
    }
}
