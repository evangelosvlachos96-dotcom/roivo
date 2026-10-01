using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBankingFieldsToBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_BankAccountId",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_BusinessId",
                table: "BankAccounts");

            migrationBuilder.AddColumn<string>(
                name: "BankingAccessTokenEncrypted",
                table: "Businesses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BankingConsentExpiresAt",
                table: "Businesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BankingFailureEmailSentAt",
                table: "Businesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BankingFirstFailureAt",
                table: "Businesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankingLastFailureReason",
                table: "Businesses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankingProviderName",
                table: "Businesses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankingSyncErrorCount",
                table: "Businesses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBankingSyncAt",
                table: "Businesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "BankTransactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "BankTransactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CounterpartyName",
                table: "BankTransactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CounterpartyIban",
                table: "BankTransactions",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Iban",
                table: "BankAccounts",
                type: "character varying(34)",
                maxLength: 34,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BankName",
                table: "BankAccounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ExternalAccountUid",
                table: "BankAccounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_BankingAccessTokenEncrypted",
                table: "Businesses",
                column: "BankingAccessTokenEncrypted",
                filter: "\"BankingAccessTokenEncrypted\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_BankAccountId_ExternalId",
                table: "BankTransactions",
                columns: new[] { "BankAccountId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_BusinessId_ExternalAccountUid",
                table: "BankAccounts",
                columns: new[] { "BusinessId", "ExternalAccountUid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_BankingAccessTokenEncrypted",
                table: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_BankAccountId_ExternalId",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_BusinessId_ExternalAccountUid",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "BankingAccessTokenEncrypted",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "BankingConsentExpiresAt",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "BankingFailureEmailSentAt",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "BankingFirstFailureAt",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "BankingLastFailureReason",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "BankingProviderName",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "BankingSyncErrorCount",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "LastBankingSyncAt",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ExternalAccountUid",
                table: "BankAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "BankTransactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "BankTransactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "CounterpartyName",
                table: "BankTransactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CounterpartyIban",
                table: "BankTransactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(34)",
                oldMaxLength: 34,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Iban",
                table: "BankAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(34)",
                oldMaxLength: 34);

            migrationBuilder.AlterColumn<string>(
                name: "BankName",
                table: "BankAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_BankAccountId",
                table: "BankTransactions",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_BusinessId",
                table: "BankAccounts",
                column: "BusinessId");
        }
    }
}
