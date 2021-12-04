using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;
using Microsoft.AspNetCore.Http;

namespace RpgApi.Controllers
{
    [Authorize(Roles = "Jogador,Admin")]
    [ApiController]
    [Route("[controller]")]
    public class PersonagensController : ControllerBase
    {
        private readonly DataContext _context; //declaração
        private readonly IHttpContextAccessor _httpContextAcessor;

        public PersonagensController(DataContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context; 
            _httpContextAcessor = httpContextAccessor; //inicialização do atributo
        }
        [AllowAnonymous]
        [HttpGet("{id}")] //buscar pelo id
        public async Task<IActionResult> GetSingle(int id)
        {
            try
            {
                 Personagem p = await _context.Personagens.Include(u => u.Usuario)
                 .Include(ar => ar.Arma)
                 .Include(ph => ph.PersonagemHabilidades)
                 .ThenInclude(h => h.Habilidade)
                 .FirstOrDefaultAsync(pBusca => pBusca.Id == id);
                 return Ok(p);
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetAll")]
        public async Task<IActionResult> Get()
        {
            try
            {
                 List<Personagem> lista = await _context.Personagens.ToListAsync();
                 return Ok(lista);
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetByUser")]
        public async Task<IActionResult> GetByUserAsync()
        {
            try
            {
                int id = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value);

                List<Personagem> lista = await _context.Personagens
                    .Where(u => u.Usuario.Id == id).ToListAsync();
                return Ok(lista);
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(Personagem novoPersonagem)
        {
            try
            {
                if(novoPersonagem.PontosVida > 100)
                {
                    throw new System.Exception("Pontos de vida não podem ser maior que 100.");
                }//Validação, situação que não pode acontecer

                /*int usuarioId = int.Parse(_httpContextAcessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
                novoPersonagem.Usuario = _context.Usuarios.FirstOrDefault(uBusca => uBusca.Id == usuarioId);*/
                novoPersonagem.Usuario = _context.Usuarios.FirstOrDefault(uBusca => uBusca.Id == ObterUsuarioId());

                 await _context.Personagens.AddAsync(novoPersonagem);
                 await _context.SaveChangesAsync();
                 return Ok(novoPersonagem.Id);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update(Personagem novoPersonagem)
        {
            try
            {
                if(novoPersonagem.PontosVida > 100)
                {
                    throw new System.Exception("Pontos de vida não podem ser maior que 100.");
                }//Validação sempre antes do update, para que a validação ocorra antes.
                /*int usuarioId = int.Parse(_httpContextAcessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
                novoPersonagem.Usuario = _context.Usuarios.FirstOrDefault(uBusca => uBusca.Id == usuarioId);*/
                novoPersonagem.Usuario = _context.Usuarios.FirstOrDefault(uBusca => uBusca.Id == ObterUsuarioId());

                 _context.Personagens.Update(novoPersonagem);
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
                 Personagem pRemover = await _context.Personagens.FirstOrDefaultAsync(p => p.Id == id);
                 _context.Personagens.Remove(pRemover);
                 int linhasAfetadas = await _context.SaveChangesAsync();

                 return Ok(linhasAfetadas);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private int ObterUsuarioId()
        {
            return int.Parse(_httpContextAcessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        private string ObterPerfilUsuario()
        {
            return _httpContextAcessor.HttpContext.User.FindFirstValue(ClaimTypes.Role);
        }

        [HttpGet("GetByPerfil")]
        public async Task<IActionResult> GetByPerfilAsync()
        {
            try
            {
                List<Personagem> lista = new List<Personagem>();

                if(ObterPerfilUsuario() == "Admin")
                {
                    lista = await _context.Personagens.ToListAsync();
                }
                else
                {
                    lista = await _context.Personagens
                    .Where(p => p.Usuario.Id == ObterUsuarioId()).ToListAsync();
                }
                return Ok(lista);
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("RestaurarPontosVida")] 
        public async Task<IActionResult> RestaurarPontosVidaAsync(Personagem p)
        {
            try
            {
                int linhasAfetadas = 0; 
                Personagem pEncontrado = await _context.Personagens.FirstOrDefaultAsync(pBusca => pBusca.Id == p.Id); 
                pEncontrado.PontosVida = 100;

                bool atualizou = await TryUpdateModelAsync<Personagem>(pEncontrado, "p", pAtualizar => pAtualizar.PontosVida);

                // EF vai detectar e atualizar apenas as colunas que foram alteradas.
                if (atualizou) linhasAfetadas = await _context.SaveChangesAsync();

                return Ok(linhasAfetadas);
            }
            catch (System.Exception ex)
            {
                
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("ZerarRanking")] 
        public async Task<IActionResult> ZerarRankingAsync(Personagem p)         
        { 
	        try            
	        { 
		        Personagem pEncontrado = await _context.Personagens.FirstOrDefaultAsync(pBusca => pBusca.Id == p.Id); 
		        pEncontrado.Disputas = 0; 
		        pEncontrado.Vitorias = 0; 
		        pEncontrado.Derrotas = 0; 
		        int linhasAfetadas = 0; 

		        bool atualizou = await TryUpdateModelAsync<Personagem>(pEncontrado, "p", 
                    pAtualizar => pAtualizar.Disputas, 
                    pAtualizar => pAtualizar.Vitorias, 
                    pAtualizar => pAtualizar.Derrotas); 
		        // EF vai detectar e atualizar apenas as colunas que foram alteradas.
		        if (atualizou) 
			        linhasAfetadas = await _context.SaveChangesAsync(); 

		        return Ok(linhasAfetadas);             
	        } 
	        catch (System.Exception ex)             
	        { 
		        return BadRequest(ex.Message);             
	        }         
        }
        [HttpPut("ZerarRankingRestaurarVidas")] 
        public async Task<IActionResult> ZerarRankingRestaurarVidasAsync()         
        { 
	        try            
	        { 
		        List<Personagem> lista = await _context.Personagens.ToListAsync(); 
		        foreach (Personagem p in lista)                 
		        { 
			        await ZerarRankingAsync(p); 
			        await RestaurarPontosVidaAsync(p);                 
		        } 
		        return Ok();             
	        } 
	        catch (System.Exception ex)             
	        { 
		        return BadRequest(ex.Message);            
 	        }         
        }
    }
}