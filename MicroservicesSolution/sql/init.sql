-- sql/init.sql
IF DB_ID('PedidosDb') IS NULL
BEGIN
  CREATE DATABASE PedidosDb;
END
GO
USE PedidosDb;
IF OBJECT_ID('dbo.Cliente') IS NULL
BEGIN
  CREATE TABLE dbo.Cliente(
    IdCliente INT PRIMARY KEY,
    NombreCliente VARCHAR(100) NOT NULL
  );
  INSERT INTO dbo.Cliente(IdCliente, NombreCliente) VALUES (1,'Cliente Demo');
END
IF OBJECT_ID('dbo.Pedido') IS NULL
BEGIN
  CREATE TABLE dbo.Pedido(
    IdPedido INT IDENTITY(1,1) PRIMARY KEY,
    FechaPedido DATETIME NOT NULL DEFAULT(GETUTCDATE()),
    IdCliente INT NOT NULL FOREIGN KEY REFERENCES dbo.Cliente(IdCliente),
    MontoPedido DECIMAL(9,2) NOT NULL
  );
END
