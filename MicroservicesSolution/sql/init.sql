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
    MontoPedido DECIMAL(9,2) NOT NULL,
    FormaPago INT NULL  
);
END
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'PagosDB')
BEGIN
    CREATE DATABASE PagosDB;
    PRINT 'Base de datos PagosDB creada.';
END
GO

USE PagosDB;
GO

CREATE TABLE dbo.Pago(
    IdPago INT IDENTITY(1,1) PRIMARY KEY,
    FechaPago DATETIME NOT NULL DEFAULT(GETUTCDATE()),
    IdCliente INT NOT NULL,
    FormaPago INT NOT NULL, -- 1=Efectivo, 2=TDC, 3=TDD
    IdPedido INT NOT NULL,
    MontoPago DECIMAL(9,2) NOT NULL,
    EstadoPago VARCHAR(20) DEFAULT 'Completado', -- Opcional: Completado, Pendiente, Fallido
    FechaCreacion DATETIME DEFAULT GETUTCDATE()
);
GO

-- Crear índices para mejor rendimiento
CREATE INDEX IX_Pago_IdCliente ON dbo.Pago(IdCliente);
CREATE INDEX IX_Pago_IdPedido ON dbo.Pago(IdPedido);
GO