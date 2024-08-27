using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using CoreBot.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreBot.Models
{
public partial class FinacleSqldbContext : DbContext
{
        private readonly IConfiguration _configuration;
        private readonly ILogger<FinacleSqldbContext> _logger;
        
        public FinacleSqldbContext(DbContextOptions<FinacleSqldbContext> options
            ,IConfiguration configuration, ILogger<FinacleSqldbContext> logger
        )
        : base(options)
        {
            _configuration = configuration;
            _logger = logger;

        }

    public virtual DbSet<CustomerDetail> CustomerDetails { get; set; }

    public virtual DbSet<Eodpublisher> Eodpublishers { get; set; }

    public virtual DbSet<TradeFlow> TradeFlows { get; set; }

    public virtual DbSet<TradingBook> TradingBooks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configuration["SQLConStr"];
            if (string.IsNullOrEmpty(connectionString))
            { 
                _logger.LogError("SQLConStr is null or empty. Ensure the secret is correctly set in Azure Key Vault.");
                throw new InvalidOperationException("SQLConStr is not configured.");
            }
            optionsBuilder.UseSqlServer(connectionString);
    } 


        protected override void OnModelCreating(ModelBuilder modelBuilder)
    {    

        modelBuilder.Entity<CustomerDetail>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.CustCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("custCode");
            entity.Property(e => e.CustLivePosition).HasColumnName("cust_Live_Position");
            entity.Property(e => e.CustStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("custStatus");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
        });

        modelBuilder.Entity<Eodpublisher>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("EODPublisher");

            entity.Property(e => e.EodDate).HasColumnName("eodDate");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.PubStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("pubStatus");
            entity.Property(e => e.SystemName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("systemName");
        });
        
        modelBuilder.Entity<TradeFlow>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TradeFlow");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.LoadDate).HasColumnName("loadDate");
            entity.Property(e => e.LoadStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("loadStatus");
            entity.Property(e => e.TradeId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("trade_id");
        });

        modelBuilder.Entity<TradingBook>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TradingBook");

            entity.Property(e => e.BookLivePosition).HasColumnName("book_Live_Position");
            entity.Property(e => e.BookName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("bookName");
            entity.Property(e => e.BookStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("bookStatus");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
        });

       OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
}