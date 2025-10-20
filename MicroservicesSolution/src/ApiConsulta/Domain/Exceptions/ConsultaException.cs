using System.Net;

namespace ApiConsulta.Domain.Exceptions
{
    // Base exception with additional context
    public class ConsultaException : Exception
    {
        public string ErrorCode { get; }
        public HttpStatusCode StatusCode { get; }
        public DateTime Timestamp { get; }
        public string UserMessage { get; }

        public ConsultaException(
            string message, 
            string errorCode = "CONSULTA_ERROR",
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
            string userMessage = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
            Timestamp = DateTime.UtcNow;
            UserMessage = userMessage ?? "Ocurrió un error procesando la consulta";
        }

        // Helper method for creating consistent error responses
        public virtual object ToErrorResponse()
        {
            return new
            {
                ErrorCode,
                Message = Message,
                UserMessage,
                Timestamp,
                Details = InnerException?.Message
            };
        }
    }

    // Specific business exceptions
    public class ConsultaNoEncontradaException : ConsultaException
    {
        public int IdPedido { get; }

        public ConsultaNoEncontradaException(int idPedido) 
            : base(
                message: $"Consulta para el pedido {idPedido} no encontrada",
                errorCode: "CONSULTA_NO_ENCONTRADA",
                statusCode: HttpStatusCode.NotFound,
                userMessage: $"No se encontró información para el pedido {idPedido}")
        {
            IdPedido = idPedido;
        }

        public override object ToErrorResponse()
        {
            return new
            {
                ErrorCode,
                Message,
                UserMessage,
                IdPedido,
                Timestamp,
                Suggestions = new[] 
                {
                    "Verifique el número de pedido",
                    "Confirme que el pedido existe en el sistema",
                    "Contacte al soporte técnico si el problema persiste"
                }
            };
        }
    }

    public class EventoPagoInvalidoException : ConsultaException
    {
        public string TipoEvento { get; }
        public string DatosEvento { get; }

        public EventoPagoInvalidoException(
            string message, 
            string tipoEvento = null, 
            string datosEvento = null,
            Exception innerException = null)
            : base(
                message: message,
                errorCode: "EVENTO_PAGO_INVALIDO",
                statusCode: HttpStatusCode.BadRequest,
                userMessage: "El evento de pago recibido no es válido",
                innerException: innerException)
        {
            TipoEvento = tipoEvento;
            DatosEvento = datosEvento;
        }

        public override object ToErrorResponse()
        {
            return new
            {
                ErrorCode,
                Message,
                UserMessage,
                TipoEvento,
                Timestamp,
                Details = new 
                {
                    InnerException = InnerException?.Message,
                    DatosEvento = DatosEvento?.Substring(0, Math.Min(DatosEvento?.Length ?? 0, 100))
                }
            };
        }
    }

    // Additional specialized exceptions
    public class KafkaCommunicationException : ConsultaException
    {
        public string Topic { get; }
        public string Operation { get; }

        public KafkaCommunicationException(
            string topic, 
            string operation, 
            Exception innerException = null)
            : base(
                message: $"Error de comunicación con Kafka durante {operation} en topic {topic}",
                errorCode: "KAFKA_COMMUNICATION_ERROR",
                statusCode: HttpStatusCode.ServiceUnavailable,
                userMessage: "Error de comunicación con el sistema de mensajería",
                innerException: innerException)
        {
            Topic = topic;
            Operation = operation;
        }
    }

    public class ConsultaBusinessRuleException : ConsultaException
    {
        public string RuleName { get; }
        public object RuleData { get; }

        public ConsultaBusinessRuleException(
            string ruleName,
            string message,
            object ruleData = null,
            Exception innerException = null)
            : base(
                message: message,
                errorCode: "BUSINESS_RULE_VIOLATION",
                statusCode: HttpStatusCode.Conflict,
                userMessage: "La operación no cumple con las reglas de negocio",
                innerException: innerException)
        {
            RuleName = ruleName;
            RuleData = ruleData;
        }
    }

    public class DatabaseUnavailableException : ConsultaException
    {
        public string DatabaseName { get; }
        public string Operation { get; }

        public DatabaseUnavailableException(
            string databaseName,
            string operation,
            Exception innerException = null)
            : base(
                message: $"Base de datos {databaseName} no disponible para operación: {operation}",
                errorCode: "DATABASE_UNAVAILABLE",
                statusCode: HttpStatusCode.ServiceUnavailable,
                userMessage: "El sistema de base de datos no está disponible temporalmente",
                innerException: innerException)
        {
            DatabaseName = databaseName;
            Operation = operation;
        }
    }

    // Validation exceptions
    public class ConsultaValidationException : ConsultaException
    {
        public Dictionary<string, string[]> ValidationErrors { get; }

        public ConsultaValidationException(Dictionary<string, string[]> validationErrors)
            : base(
                message: "Error de validación en los datos de consulta",
                errorCode: "VALIDATION_ERROR",
                statusCode: HttpStatusCode.BadRequest,
                userMessage: "Por favor verifique los datos ingresados")
        {
            ValidationErrors = validationErrors;
        }

        public override object ToErrorResponse()
        {
            return new
            {
                ErrorCode,
                Message,
                UserMessage,
                Timestamp,
                ValidationErrors
            };
        }
    }
}