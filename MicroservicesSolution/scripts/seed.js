use ApiConsultaDB;
db.Consultas.insertMany([
  {
    IdPedido: 1,
    NombreCliente: "Cliente 1",
    IdPago: 101,
    FormaPago: 1
  },
  {
    IdPedido: 2,
    NombreCliente: "Cliente 2",
    IdPago: 102,
    FormaPago: 2
  }
]);
