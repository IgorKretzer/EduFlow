# EduFlow — como subir tudo no Windows (PowerShell)

> **Deploy Vercel + Oracle:** guias em `docs/` no seu PC (arquivos locais, fora do GitHub).

## URLs corretas

| O quê | URL | O que você deve ver |
|-------|-----|---------------------|
| **Front** | http://localhost:3000/login | Tela de login EduFlow |
| **API (Swagger)** | http://localhost:8080/swagger | Documentação da API |
| **API raiz** | http://localhost:8080/ | **404 é normal** — não é interface visual |
| **Rabbit** | http://127.0.0.1:15672 | Painel RabbitMQ |

Se o front disser `Port 3000 is in use, trying 3001`, use **http://localhost:3001/login**.

---

## Antes de começar (uma vez)

```powershell
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
```

Para `npm` no PowerShell (se der erro de script):

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

Ou use sempre: `& "C:\Program Files\nodejs\npm.cmd"` no lugar de `npm`.

---

## Terminal 1 — API

```powershell
cd "C:\Users\Aninha\OneDrive\Área de Trabalho\EduFlow"
dotnet run --project src\EduFlow.Api
```

Espere: `Now listening on: http://localhost:8080`

Teste: abra http://localhost:8080/swagger

---

## Terminal 2 — Workers

```powershell
cd "C:\Users\Aninha\OneDrive\Área de Trabalho\EduFlow"
dotnet run --project src\EduFlow.Workers
```

Espere: `Workers HTTP health em http://localhost:5055/health/live`

---

## Terminal 3 — Front

```powershell
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
cd "C:\Users\Aninha\OneDrive\Área de Trabalho\EduFlow\src\frontend"
& "C:\Program Files\nodejs\npm.cmd" install
& "C:\Program Files\nodejs\npm.cmd" run dev
```

Espere: `Ready on http://localhost:3000` (ou **3001**).

Abra: http://localhost:3000/login → **Criar ambiente demo**

---

## Se a tela ficar branca

1. Confirme a **porta** no terminal (`3000` ou `3001`).
2. Mate processos antigos na porta 3000:

```powershell
netstat -ano | findstr :3000
# Anote o PID na última coluna, depois:
taskkill /PID NUMERO_DO_PID /F
```

3. Limpe cache do Next e suba de novo:

```powershell
cd "C:\Users\Aninha\OneDrive\Área de Trabalho\EduFlow\src\frontend"
Remove-Item -Recurse -Force .next -ErrorAction SilentlyContinue
& "C:\Program Files\nodejs\npm.cmd" run dev
```

4. No navegador: **F12** → aba **Console** → copie erros em vermelho.

5. Não abra `http://localhost:8080` esperando o site — use **Swagger** ou o **front na 3000**.

---

## Checklist rápido

- [ ] SQL Server rodando
- [ ] RabbitMQ portas 5672 e 15672
- [ ] API no 8080 (Swagger abre)
- [ ] Workers sem erro de exchange
- [ ] Front `npm run dev` com Ready
- [ ] Login em `/login` (não só `/`)
