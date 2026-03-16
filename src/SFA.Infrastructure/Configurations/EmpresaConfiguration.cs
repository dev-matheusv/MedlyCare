using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresa");

        builder.HasKey(x => x.CodEmpresa);

        builder.Property(x => x.CodEmpresa)
            .HasColumnName("cod_empresa")
            .HasColumnType("integer")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(x => x.Id).IsUnique();

        builder.Property(x => x.RazaoSocial)
            .HasColumnName("razao_social")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Documento)
            .HasColumnName("documento")
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.InscricaoEstadualRg)
            .HasColumnName("inscricao_estadual_rg")
            .HasMaxLength(30);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Telefone)
            .HasColumnName("telefone")
            .HasMaxLength(20);

        builder.Property(x => x.CelularWhatsapp)
            .HasColumnName("celular_whatsapp")
            .HasMaxLength(20);

        builder.Property(x => x.UtilizarCelularParaEnvioMensagens)
            .HasColumnName("utilizar_celular_para_envio_mensagens")
            .IsRequired();

        builder.Property(x => x.Endereco)
            .HasColumnName("endereco")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NumeroImovel)
            .HasColumnName("numero_imovel")
            .IsRequired();

        builder.Property(x => x.Bairro)
            .HasColumnName("bairro")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Cidade)
            .HasColumnName("cidade")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Uf)
            .HasColumnName("uf")
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(x => x.Cep)
            .HasColumnName("cep")
            .IsRequired()
            .HasMaxLength(12);

        builder.Property(x => x.Pais)
            .HasColumnName("pais")
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(x => x.ResponsavelClinica)
            .HasColumnName("responsavel_clinica")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.PathLogotipo)
            .HasColumnName("path_logotipo")
            .HasMaxLength(500);

        builder.Property(x => x.Cnae)
            .HasColumnName("cnae")
            .HasMaxLength(20);

        builder.Property(x => x.RedesSociais)
            .HasColumnName("redes_sociais")
            .HasMaxLength(500);

        builder.Property(x => x.Ativa)
            .HasColumnName("ativa")
            .IsRequired();

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasDefaultValueSql("now()");

        builder.HasIndex(x => x.RazaoSocial);
        builder.HasIndex(x => x.Documento).IsUnique();
        builder.HasIndex(x => x.Email);
    }
}
