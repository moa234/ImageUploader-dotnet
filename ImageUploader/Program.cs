using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Content("""
                                      <html>
                                      <head>
                                          <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous">
                                      </head>
                                      <body>
                                          <div class="container">
                                              <h1>Upload Image</h1>
                                              <form action="/upload" method="post" enctype="multipart/form-data">
                                                  <input class="form-control mt-4" type="text" placeholder="Title" name="title" required>
                                                  <input class="form-control form-control-sm mt-2" type="file" name="file" required>
                                                  <br/>
                                                  <button type="submit" class="btn btn-primary">Upload</button>
                                              </form>
                                          </div>
                                      </body>
                                      </html>
                                      """, "text/html"));

app.Run();
return;

bool IsImage(IFormFile file)
{
    var allowedContentTypes = new[] { "image/png", "image/jpeg", "image/gif" };
    return allowedContentTypes.Contains(file.ContentType);
}
