using ProjetoIMC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDataContext>();

builder.Services.AddDbContext<AppDataContext>();

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy("AcessoTotal",
            builder => builder.
                AllowAnyOrigin().
                AllowAnyHeader().
                AllowAnyMethod());
    }
);

var app = builder.Build();


app.MapPost("/aluno/cadastrar", async (AppDbContext db, Aluno aluno) =>
{
    if (db.Alunos.Any(a => a.Nome == aluno.Nome))
        return Results.BadRequest("Aluno já cadastrado com esse nome.");

    db.Alunos.Add(aluno);
    await db.SaveChangesAsync();
    return Results.Ok(aluno);
});

app.MapPost("/imc/cadastrar", async (AppDbContext db, int alunoId, float altura, float peso) =>
{
    var aluno = await db.Alunos.FindAsync(alunoId);
    if (aluno == null)
        return Results.NotFound("Aluno não encontrado.");

    var imc = new IMC
    {
        AlunoId = alunoId,
        Altura = altura,
        Peso = peso,
        Valor = peso / (altura * altura),
        Classificacao = CalcularClassificacao(peso / (altura * altura)),
        DataCriacao = DateTime.Now
    };

    db.IMCs.Add(imc);
    await db.SaveChangesAsync();
    return Results.Ok(imc);
});

app.MapGet("/imc/listar", async (AppDbContext db) => await db.IMCs.Include(i => i.Aluno).ToListAsync());

app.MapGet("/imc/listarporaluno/{alunoId}", async (AppDbContext db, int alunoId) =>
{
    var imcs = await db.IMCs.Where(i => i.AlunoId == alunoId).Include(i => i.Aluno).ToListAsync();
    if (!imcs.Any())
        return Results.NotFound("Nenhum IMC encontrado para esse aluno.");

    return Results.Ok(imcs);
});

app.MapPut("/imc/alterar/{id}", async (AppDbContext db, int id, IMC updatedImc) =>
{
    var imc = await db.IMCs.FindAsync(id);
    if (imc == null)
        return Results.NotFound("IMC não encontrado.");

    imc.Altura = updatedImc.Altura;
    imc.Peso = updatedImc.Peso;
    imc.Valor = updatedImc.Peso / (updatedImc.Altura * updatedImc.Altura);
    imc.Classificacao = CalcularClassificacao(imc.Valor);
    imc.DataCriacao = DateTime.Now;

    await db.SaveChangesAsync();
    return Results.Ok(imc);
});

static string CalcularClassificacao(float imc)
{
    if (imc < 18.5) return "Magreza";
    if (imc >= 18.5 && imc < 24.9) return "Normal";
    if (imc >= 25 && imc < 29.9) return "Sobrepeso";
    if (imc >= 30 && imc < 39.9) return "Obesidade";
    return "Obesidade Grave";
}

app.Run();
