using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiConsulta.Domain
{
    public class Consulta
    {
         [BsonId] // ✅ ESTE ES EL ID PRINCIPAL DE MONGO
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } // ✅ USAR SOLO ESTE ID

        [BsonElement("idPedido")]
        public int IdPedido { get; set; }

        [BsonElement("nombreCliente")]
        public string NombreCliente { get; set; } = string.Empty;

        [BsonElement("idPago")]
        public int IdPago { get; set; }

        [BsonElement("montoPago")]
        public decimal MontoPago { get; set; }

        [BsonElement("formaPago")]
        public int FormaPago { get; set; }

        [BsonElement("fechaConsulta")]
        public DateTime FechaConsulta { get; set; }

        [BsonElement("fechaProcesamiento")]
        public DateTime FechaProcesamiento { get; set; }

        [BsonElement("estado")]
        public string Estado { get; set; } = "Procesado";

        [BsonElement("topicoKafka")]
        public string TopicoKafka { get; set; } = "pedidos-procesados";

        [BsonElement("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public Consulta() 
        { 
             Id = Guid.NewGuid(); // ✅ GENERAR GUID EN EL CONSTRUCTOR

         }

        public Consulta(int idPedido, string nombreCliente, int idPago, decimal montoPago, int formaPago)
            : this() // ✅ LLAMAR AL CONSTRUCTOR POR DEFECTO
        {
            if (idPedido == 0)
                throw new ArgumentException($"ID de pedido inválido: {idPedido}");

            if (string.IsNullOrWhiteSpace(nombreCliente))
                throw new ArgumentException("Nombre de cliente no puede estar vacío");

            if (idPago <= 0)
                throw new ArgumentException($"ID de pago inválido: {idPago}");

            if (montoPago <= 0)
                throw new ArgumentException($"Monto de pago inválido: {montoPago}");

            IdPedido = idPedido;
            NombreCliente = nombreCliente;
            IdPago = idPago;
            MontoPago = montoPago;
            FormaPago = formaPago;
            FechaConsulta = DateTime.UtcNow;
            FechaProcesamiento = DateTime.UtcNow;
        }

        public void ActualizarMetadata(Dictionary<string, object> metadata)
        {
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        public void ActualizarEstado(string nuevoEstado)
        {
            if (!string.IsNullOrWhiteSpace(nuevoEstado))
            {
                Estado = nuevoEstado;
                FechaProcesamiento = DateTime.UtcNow;
            }
        }

        public void MarcarComoProcesado()
        {
            Estado = "Procesado";
            FechaProcesamiento = DateTime.UtcNow;
        }
    }
}