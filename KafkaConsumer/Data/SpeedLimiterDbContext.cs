using System;
using System.Collections.Generic;
using KafkaConsumer.Models;
using Microsoft.EntityFrameworkCore;

namespace KafkaConsumer.Data;

public partial class SpeedLimiterDbContext : DbContext
{
    public SpeedLimiterDbContext()
    {
    }

    public SpeedLimiterDbContext(DbContextOptions<SpeedLimiterDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<Efmigrationshistory> Efmigrationshistories { get; set; }

    public virtual DbSet<Fitting> Fittings { get; set; }

    public virtual DbSet<Fittingagent> Fittingagents { get; set; }

    public virtual DbSet<Limiter> Limiters { get; set; }

    public virtual DbSet<Make> Makes { get; set; }

    public virtual DbSet<Model> Models { get; set; }

    public virtual DbSet<Owner> Owners { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Station> Stations { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseMySQL("server=localhost;User Id=root;password=root;database=speedlimiter;port=3308");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("devices");

            entity.HasIndex(e => e.Imei, "ix_devices_imei").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasMaxLength(6)
                .HasColumnName("createdat");
            entity.Property(e => e.Disabled).HasColumnName("disabled");
            entity.Property(e => e.Imei).HasColumnName("imei");
            entity.Property(e => e.Lastupdated)
                .HasMaxLength(6)
                .HasColumnName("lastupdated");
            entity.Property(e => e.Latestvehicleid).HasColumnName("latestvehicleid");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Positionid).HasColumnName("positionid");
            entity.Property(e => e.Serialno)
                .HasMaxLength(30)
                .HasColumnName("serialno");
        });

        modelBuilder.Entity<Efmigrationshistory>(entity =>
        {
            entity.HasKey(e => e.Migrationid).HasName("PRIMARY");

            entity.ToTable("__efmigrationshistory");

            entity.Property(e => e.Migrationid)
                .HasMaxLength(150)
                .HasColumnName("migrationid");
            entity.Property(e => e.Productversion)
                .HasMaxLength(32)
                .HasColumnName("productversion");
        });

        modelBuilder.Entity<Fitting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("fittings");

            entity.HasIndex(e => e.Certificateno, "ix_fittings_certificateno").IsUnique();

            entity.HasIndex(e => e.Stationid, "ix_fittings_stationid");

            entity.HasIndex(e => e.Vehicleid, "ix_fittings_vehicleid");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Certificateno)
                .HasMaxLength(30)
                .HasColumnName("certificateno");
            entity.Property(e => e.Expirydate)
                .HasMaxLength(6)
                .HasColumnName("expirydate");
            entity.Property(e => e.Fittingagentid).HasColumnName("fittingagentid");
            entity.Property(e => e.Fittingdate)
                .HasMaxLength(6)
                .HasColumnName("fittingdate");
            entity.Property(e => e.Stationid).HasColumnName("stationid");
            entity.Property(e => e.Vehicleid).HasColumnName("vehicleid");

            entity.HasOne(d => d.Station).WithMany(p => p.Fittings)
                .HasForeignKey(d => d.Stationid)
                .HasConstraintName("fk_fittings_stations_stationid");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Fittings)
                .HasForeignKey(d => d.Vehicleid)
                .HasConstraintName("fk_fittings_vehicles_vehicleid");
        });

        modelBuilder.Entity<Fittingagent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("fittingagents");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Businessregistrationno)
                .HasMaxLength(30)
                .HasColumnName("businessregistrationno");
            entity.Property(e => e.Email)
                .HasMaxLength(60)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<Limiter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("limiters");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Make>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("makes");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Model>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("models");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Owner>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("owners");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("positions");

            entity.HasIndex(e => new { e.Gpsdatetime, e.Deviceid }, "ix_positions_gpsdatetime_deviceid");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Altitude).HasColumnName("altitude");
            entity.Property(e => e.Deviceid).HasColumnName("deviceid");
            entity.Property(e => e.Event).HasColumnName("event");
            entity.Property(e => e.Gpsdatetime)
                .HasMaxLength(6)
                .HasColumnName("gpsdatetime");
            entity.Property(e => e.Ignitionstatus).HasColumnName("ignitionstatus");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            entity.Property(e => e.Powersignal).HasColumnName("powersignal");
            entity.Property(e => e.Satellites).HasColumnName("satellites");
            entity.Property(e => e.Servertime)
                .HasMaxLength(6)
                .HasColumnName("servertime");
            entity.Property(e => e.Speed).HasColumnName("speed");
        });

        modelBuilder.Entity<Station>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("stations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Lockedout).HasColumnName("lockedout");
            entity.Property(e => e.Loggedin).HasColumnName("loggedin");
            entity.Property(e => e.Logintime)
                .HasMaxLength(6)
                .HasColumnName("logintime");
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.Username)
                .HasMaxLength(20)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("vehicles");

            entity.HasIndex(e => e.Deviceid, "ix_vehicles_deviceid");

            entity.HasIndex(e => e.Makeid, "ix_vehicles_makeid");

            entity.HasIndex(e => e.Modelid, "ix_vehicles_modelid");

            entity.HasIndex(e => e.Registrationno, "ix_vehicles_registrationno").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Chassisno)
                .HasMaxLength(20)
                .HasColumnName("chassisno");
            entity.Property(e => e.Deviceid).HasColumnName("deviceid");
            entity.Property(e => e.Latestfittingid).HasColumnName("latestfittingid");
            entity.Property(e => e.Makeid).HasColumnName("makeid");
            entity.Property(e => e.Modelid).HasColumnName("modelid");
            entity.Property(e => e.Registrationno)
                .HasMaxLength(8)
                .HasColumnName("registrationno");

            entity.HasOne(d => d.Device).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.Deviceid)
                .HasConstraintName("fk_vehicles_devices_deviceid");

            entity.HasOne(d => d.Make).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.Makeid)
                .HasConstraintName("fk_vehicles_makes_makeid");

            entity.HasOne(d => d.Model).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.Modelid)
                .HasConstraintName("fk_vehicles_models_modelid");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
