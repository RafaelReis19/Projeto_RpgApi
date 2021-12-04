using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DisputasController : ControllerBase
    {
        private readonly DataContext _context;
        public DisputasController (DataContext context)
        {
            _context = context;
        }
    
        [HttpPost("Arma")]
        public async Task<IActionResult> AtaqueComArmaAsync(Disputa d)
        {
            try
            {
                Personagem atacante = await _context.Personagens.Include(p => p.Arma)
                .FirstOrDefaultAsync(p => p.Id == d.AtacanteId);

                Personagem oponente = await _context.Personagens.FirstOrDefaultAsync(p => p.Id == d.OponenteId);

                int dano = atacante.Arma.Dano + (new Random().Next(atacante.Forca));
                dano = dano - new Random().Next(oponente.Defesa);
                if(dano > 0)
                {
                    oponente.PontosVida = oponente.PontosVida - dano;
                }
                if(oponente.PontosVida <= 0)
                {
                    d.Narracao = $"{oponente.Nome} foi derrotado";
                }
                _context.Personagens.Update(oponente);
                await _context.SaveChangesAsync();

                StringBuilder dados = new StringBuilder();
                dados.AppendFormat(" Atacante: {0}. ", atacante.Nome);
                dados.AppendFormat(" Oponente: {0}. ", oponente.Nome);
                dados.AppendFormat(" Pontos de vida do atacante: {0}. ", atacante.PontosVida);
                dados.AppendFormat(" Pontos de vida do oponente: {0}. ", oponente.PontosVida);
                dados.AppendFormat(" Dano: {0} ", dano);

                d.Narracao += dados.ToString();

                _context.Disputas.Add(d);
                _context.SaveChanges();

                return Ok(d);
            }
            catch (System.Exception ex)
            {
            
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("Habilidade")]
        public async Task<IActionResult> AtaqueComHabilidadeAsync(Disputa d)
        {
            try
            {
                Personagem atacante = await _context.Personagens
                .Include(p => p.PersonagemHabilidades)
                .ThenInclude(ph => ph.Habilidade)
                .FirstOrDefaultAsync(p => p.Id == d.AtacanteId);

                Personagem oponente = await _context.Personagens
                .FirstOrDefaultAsync(p => p.Id == d.OponenteId);

                PersonagemHabilidade ph = await _context.PersonagemHabilidades
                .Include(p => p.Habilidade)
                .FirstOrDefaultAsync(phBusca => phBusca.HabilidadeId == d.HabilidadeId);

                if(ph == null)
                    d.Narracao = $"{atacante.Nome} não possui habilidade";
                else
                {
                    int dano = ph.Habilidade.Dano + (new Random().Next(atacante.Inteligencia));
                    dano = dano - new Random().Next(oponente.Defesa);

                    if(dano > 0)
                    {
                        oponente.PontosVida -= dano;
                    }
                    if(oponente.PontosVida <= 0)
                    {
                        d.Narracao += $"{oponente.Nome} foi derrotado!";
                    }
                    _context.Personagens.Update(oponente);
                    await _context.SaveChangesAsync();

                    StringBuilder dados = new StringBuilder();
                    dados.AppendFormat(" Atacante: {0}. ", atacante.Nome);
                    dados.AppendFormat(" Oponente: {0}. ", oponente.Nome);
                    dados.AppendFormat(" Pontos de vida do atacante: {0}. ", atacante.PontosVida);
                    dados.AppendFormat(" Pontos de vida do oponente: {0}. ", oponente.PontosVida);
                    dados.AppendFormat(" Habilidade utilizada: {0}. ", ph.Habilidade.Nome);
                    dados.AppendFormat(" Dano: {0} ", dano);

                    d.Narracao += dados.ToString();

                    _context.Disputas.Add(d);
                    _context.SaveChanges();
                }
                return Ok(d);
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("PersonagemRandom")]
        public async Task<IActionResult> Sorteio()
        {
            List<Personagem> personagens =
                 await _context.Personagens.ToListAsync();
            //Sorteio com numero da quantidade de personagens - 1
            int sorteio = new Random().Next(personagens.Count);
            //busca na lista pelo indice sorteado (Não é o ID)
            Personagem p = personagens[sorteio];
            string msg =
                string.Format("Nº Sorteado {0}. Personagem: {1}", sorteio, p.Nome);
            return Ok(msg);
        }
        [HttpPost("DisputaEmGrupo")]
        public async Task<IActionResult> DisputaEmGrupoAsync(Disputa d)
        {
            try
            {
                //Busca na base dos personagens informados no parametro, incluindo armas e habilidades
                List<Personagem> personagens = await _context.Personagens.Include(p => p.Arma)
                 .Include(p => p.PersonagemHabilidades).ThenInclude(ph => ph.Habilidade)
                 .Where(p => d.ListaIdPersonagens.Contains(p.Id)).ToListAsync();

                //Contagem dos personagens vivos na lista obtida do banco de dados
                int qtdPersonagensVivos = personagens.FindAll(p => p.PontosVida > 0).Count;

                //Enquanto houver mais de um personagem vivo haverá disputa
                while(qtdPersonagensVivos > 1)
                {
                    //seleciona personagens com pontosVida positivo e faz sorteio
                    List<Personagem> atacantes = personagens.Where(p => p.PontosVida > 0).ToList();
                    Personagem atacante = atacantes[new Random().Next(atacantes.Count)];
                    d.AtacanteId = atacante.Id;

                    //Seleciona personagens com pontosVida, exceto o atacante, e faz o sorteio
                    List<Personagem> oponentes = personagens.Where(p => p.Id != atacante.Id && p.PontosVida > 0).ToList();
                    Personagem oponente = oponentes[new Random().Next(oponentes.Count)];
                    d.OponenteId = oponente.Id;

                    //declara e redefine, a cada personagem do while, o valor das variaveis que serão usadas
                    int dano = 0;
                    string ataqueUsado = string.Empty;
                    string resultado = string.Empty;

                    //Sorteia entre 0 e 1: 0 é um ataque com arma e 1 é um ataque com habilidades
                    bool ataqueUsaArma = (new Random().Next(1) == 0);
                    if(ataqueUsaArma && atacante.Arma != null)
                    {
                        //sorteio da força 
                        dano = atacante.Arma.Dano + (new Random().Next(atacante.Forca));
                        dano = dano - new Random().Next(oponente.Defesa); //sorteio da defesa.
                        ataqueUsado = atacante.Arma.Nome;
                        if (dano > 0)
                            oponente.PontosVida = oponente.PontosVida - (int)dano;
                        //Formata a mensagem
                        resultado = 
                            string.Format ("{0} atacou {1} usando {2} com o dano {3}.", atacante.Nome, oponente.Nome, ataqueUsado, dano);
                        d.Narracao += resultado; //concatena o resultado com as narrações existentes
                        d.Resultados.Add(resultado); //adiciona o resultado atual na lista de resultados
                    }
                    else if(atacante.PersonagemHabilidades.Count != 0)
                    {
                        //Realiza o sorteio entre as habilidades existentes e na linha seguinte a seleciona
                        int sorteioHabilidadeId = new Random().Next(atacante.PersonagemHabilidades.Count);
                        Habilidade habilidadeEscolhida = atacante.PersonagemHabilidades[sorteioHabilidadeId].Habilidade;
                        ataqueUsado = habilidadeEscolhida.Nome;

                        //sorteio da inteligência somado ao dano
                        dano = habilidadeEscolhida.Dano + (new Random().Next(atacante.Inteligencia));
                        dano = dano - new Random().Next(oponente.Defesa);//sorteio da defesa

                        if (dano > 0)
                            oponente.PontosVida = oponente.PontosVida - (int)dano;
                        
                        resultado = 
                            string.Format ("{0} atacou {1} usando {2} com o dano {3}.", atacante.Nome, oponente.Nome, ataqueUsado, dano);
                        d.Narracao += resultado; 
                        d.Resultados.Add(resultado); 
                    }
                    //Aqui ficará a verificação do ataque usado e se tem um personagem vivo
                    if(!string.IsNullOrEmpty(ataqueUsado))
                    {
                        atacante.Vitorias++;
                        oponente.Derrotas++;
                        oponente.Disputas++;
                        atacante.Disputas++;

                        d.Id = 0;
                        d.DataDisputa = DateTime.Now;
                        _context.Disputas.Add(d);
                        await _context.SaveChangesAsync();
                    }
                    qtdPersonagensVivos = personagens.FindAll(p => p.PontosVida > 0).Count;

                    if(qtdPersonagensVivos == 1)
                    {
                        string resultadoFinal = 
                            $"{atacante.Nome.ToUpper()} é campeão com {atacante.PontosVida} pontos de vida restantes!";
                        d.Narracao += resultadoFinal;
                        d.Resultados.Add(resultadoFinal);

                        break;
                    }
                }

                //Código após o fechamento do while, atualizará os personagens vivos, disputas, vitórias
                //e derrotas, após o final da batalha
                _context.Personagens.UpdateRange(personagens);
                await _context.SaveChangesAsync();

                return Ok(d); //retorna os dados da disputa
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("ApagarDisputas")] 
        public async Task<IActionResult> DeleteAsync()
        {
            try
            {
                List<Disputa> disputas = await _context.Disputas.ToListAsync();
                _context.Disputas.RemoveRange(disputas); 
                await _context.SaveChangesAsync();

                return Ok("Disputas apagadas");
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }
    }
}