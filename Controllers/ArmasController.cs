using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ArmasController : ControllerBase
    {
       private readonly DataContext _context;

        public ArmasController(DataContext context)
        {
            _context = context;
        }
        [HttpGet("GetAll")]
        public async Task<IActionResult> Get()
        {
            try
            {
                 List<Arma> lista = await _context.Armas.ToListAsync();
                 return Ok(lista);
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSingle(int id)
        {
            try
            {
                 Arma w = await _context.Armas.FirstOrDefaultAsync(wBusca => wBusca.Id == id);
                 return Ok(w);
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Add(Arma novoArma)
        {
            try
            {
                if(novoArma.Dano > 100)
                {
                    throw new System.Exception("O dano das armas não pode ser maior que 100.");
                }//Validação, situação que não pode acontecer
                Personagem p = await _context.Personagens.FirstOrDefaultAsync(p => p.Id == novoArma.PersonagemId);
                if(p == null)
                    throw new System.Exception("Não existe personagem com o Id informado");

                Arma buscaArma = await _context.Armas
                .FirstOrDefaultAsync(a => a.PersonagemId == novoArma.PersonagemId);

                if(buscaArma != null)
                    throw new System.Exception("O personagem selecionado já contém uma arma atribuida a ele");

                 await _context.Armas.AddAsync(novoArma);
                 await _context.SaveChangesAsync();
                 return Ok(novoArma.Id);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut]
        public async Task<IActionResult> Update(Arma novoArma)
        {
            try
            {
                if(novoArma.Dano > 100)
                {
                    throw new System.Exception("O dano das armas não pode ser maior que 100.");
                }//Validação sempre antes do update, para que a validação ocorra antes.
                Personagem p = await _context.Personagens.FirstOrDefaultAsync(p => p.Id == novoArma.PersonagemId);
                if(p == null)
                    throw new System.Exception("Não existe personagem com o Id informado");
                    
                 _context.Armas.Update(novoArma);
                 int linhasAfetadas = await _context.SaveChangesAsync();
                 return Ok(linhasAfetadas);
            }
            catch (System.Exception ex)
            { 
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                 Arma pRemover = await _context.Armas.FirstOrDefaultAsync(p => p.Id == id);
                 _context.Armas.Remove(pRemover);
                 int linhasAfetadas = await _context.SaveChangesAsync();

                 return Ok(linhasAfetadas);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}