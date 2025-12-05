using DeuEposta.Models;
using DeuEposta.Services;
using DeuEposta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FileController> _logger;

    public FileController(
        IFileService fileService,
        ILogger<FileController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? fileType = null)
    {
        var response = await _fileService.GetFilesAsync(page, pageSize, searchTerm, fileType);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            401 => Unauthorized(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseDataModel<Dosya>>> GetFile(int id)
    {
        var result = await _fileService.GetFileByIdAsync(id);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("{id}/info")]
    public async Task<ActionResult<ResponseDataModel<Services.FileInfo>>> GetFileInfo(int id)
    {
        var result = await _fileService.GetFileInfoAsync(id);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost("upload")]
    [EnableRateLimiting("Upload")]
    public async Task<ActionResult<ResponseDataModel<FileUploadResult>>> UploadFile(
        IFormFile file,
        [FromForm] string? description = null,
        [FromForm] int? announcementId = null,
        [FromForm] string? sessionId = null)
    {        
        var kullaniciId = GetCurrentUserId();
        var result = await _fileService.UploadFileAsync(file, kullaniciId, description, announcementId, sessionId);        

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost("upload-multiple")]
    [EnableRateLimiting("Upload")]
    public async Task<ActionResult<ResponseDataModel<List<FileUploadResult>>>> UploadMultipleFiles(
        List<IFormFile> files,
        [FromForm] string? description = null)
    {
        var kullaniciId = GetCurrentUserId();
        var result = await _fileService.UploadMultipleFilesAsync(files, kullaniciId, description);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var result = await _fileService.DownloadFileAsync(id);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        var fileResult = result.Data;
        if (fileResult!.FileStream != null)
        {
            // Streamed response for large files with range processing enabled
            return File(fileResult.FileStream, fileResult.ContentType, fileResult.FileName, enableRangeProcessing: true);
        }

        // Fallback for small files loaded into memory
        return File(fileResult.FileContent ?? Array.Empty<byte>(), fileResult.ContentType, fileResult.FileName);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<ActionResult<ResponseModel>> DeleteFile(int id)
    {
        var kullaniciId = GetCurrentUserId();
        var result = await _fileService.DeleteFileAsync(id, kullaniciId);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("announcement/{announcementId}")]
    public async Task<ActionResult<ResponseDataModel<List<Dosya>>>> GetAnnouncementFiles(int announcementId)
    {
        var result = await _fileService.GetAnnouncementFilesAsync(announcementId);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost("{fileId}/attach/{announcementId}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<ActionResult<ResponseModel>> AttachFileToAnnouncement(int fileId, int announcementId)
    {
        var kullaniciId = GetCurrentUserId();
        var result = await _fileService.AttachFileToAnnouncementAsync(fileId, announcementId, kullaniciId);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpDelete("{fileId}/detach/{announcementId}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<ActionResult<ResponseModel>> DetachFileFromAnnouncement(int fileId, int announcementId)
    {
        var kullaniciId = GetCurrentUserId();
        var result = await _fileService.DetachFileFromAnnouncementAsync(fileId, announcementId, kullaniciId);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ResponseModel>> ValidateFile(IFormFile file)
    {
        var result = await _fileService.ValidateFileAsync(file);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult<ResponseDataModel<List<Dosya>>>> GetSessionFiles(string sessionId)
    {
        // GÜVENLİK: Session ID'nin mevcut kullanıcıya ait olduğunu doğrula
        var kullaniciId = GetCurrentUserId();
        if (!sessionId.StartsWith($"{kullaniciId}_"))
        {
            _logger.LogWarning("Unauthorized session access attempt: User {UserId} tried to access session {SessionId}", kullaniciId, sessionId);
            return Forbid();
        }

        var result = await _fileService.GetSessionFilesAsync(sessionId);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost("session/{sessionId}/link/{announcementId}")]
    public async Task<ActionResult<ResponseModel>> LinkSessionFiles(string sessionId, int announcementId)
    {
        var kullaniciId = GetCurrentUserId();

        // GÜVENLİK: Session ID'nin mevcut kullanıcıya ait olduğunu doğrula
        if (!sessionId.StartsWith($"{kullaniciId}_"))
        {
            _logger.LogWarning("Unauthorized session link attempt: User {UserId} tried to link session {SessionId}", kullaniciId, sessionId);
            return Forbid();
        }

        var result = await _fileService.LinkSessionFilesToAnnouncementAsync(sessionId, announcementId, kullaniciId);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    // REMOVED: Gereksiz endpoint - Frontend kullanmıyor
    // FileService constructor'da SISTEM_AYARLARI'dan zaten ayarları okuyor ve field'larda saklıyor
    // Upload validasyonu FileService.UploadFileAsync() içinde yapılıyor
    // Eğer frontend'e limit bilgisi gerekirse FileService'e GetFileLimits() metodu eklenip kullanılabilir

    [HttpGet("generate-session")]
    public ActionResult<ResponseDataModel<string>> GenerateSessionId()
    {
        var kullaniciId = GetCurrentUserId();
        var sessionId = $"{kullaniciId}_{Guid.NewGuid():N}";

        _logger.LogInformation("Generated secure session ID for user {UserId}", kullaniciId);

        return Ok(ResponseDataModel<string>.SuccessResult(sessionId, "Session ID oluşturuldu"));
    }

    private int GetCurrentUserId() => HttpContextHelper.GetCurrentUserId(User);

}