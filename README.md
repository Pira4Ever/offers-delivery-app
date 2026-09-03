# Salto Ofertas (Offers Delivery App)

Aplicativo mobile multiplataforma desenvolvido com **.NET MAUI** que agrega e exibe os jornais de ofertas (flyers) dos principais supermercados e atacadistas da região de **Salto/SP**.

O app faz scraping dos sites oficiais das redes, armazena os dados localmente em SQLite e permite visualizar os folhetos em PDF ou imagens de forma prática e offline.

---

## ✨ Funcionalidades

- **Lista de mercados** com logos clicáveis
- Scraping automático dos jornais de ofertas dos seguintes mercados:
  - **Roldão Atacadista**
  - **São Vicente**
  - **Pague Menos**
  - **Tenda Atacado**
  - **Delta Supermercados**
  - **São Roque Supermercados**
- Visualização nativa de PDFs (via `MauiNativePdfView`)
- Suporte a folhetos em formato de imagens (paginados)
- Cache local com SQLite (atualização a cada 1 hora quando há internet)
- Limpeza automática de ofertas expiradas
- Atualização automática do aplicativo (download + instalação de APK)
- Interface simples e focada em usabilidade

---

## 🛠️ Tecnologias Utilizadas

| Tecnologia              | Uso                                      |
|-------------------------|------------------------------------------|
| .NET 10 + MAUI          | Framework principal                      |
| AngleSharp              | Parsing HTML (scraping)                  |
| sqlite-net-pcl          | Banco de dados local                     |
| CommunityToolkit.Maui   | Componentes e helpers                    |
| CommunityToolkit.Mvvm   | MVVM                                     |
| Eightbot.MauiNativePdfView | Visualização nativa de PDFs           |
| LaYumba.Functional      | Tratamento funcional de erros            |
| GitHub Actions          | CI/CD (build + release automática)       |

---

## 📁 Estrutura do Projeto

```
offers-delivery-app/
├── OffersDelivery/                 # Projeto MAUI (UI + lógica de apresentação)
│   ├── Platforms/                  # Código específico de plataforma
│   ├── Resources/                  # Imagens, fontes, splash, ícone
│   ├── ViewModels/                 # ViewModels (MVVM)
│   ├── MainPage.xaml               # Tela principal (lista de mercados)
│   ├── OffersPage.xaml             # Tela de visualização das ofertas
│   └── MauiProgram.cs              # Configuração de DI e serviços
│
├── OffersDelivery.Core/            # Lógica de negócio e acesso a dados
│   ├── ApiClient.cs                # Scraping de todos os mercados
│   ├── Dtos/                       # Data Transfer Objects
│   ├── Enums/                      # Enumeração de mercados
│   ├── Models/                     # Modelos (OfferModel)
│   ├── Repositories/               # OfferRepository (SQLite)
│   └── Services/                   # UpdateService (atualização do app)
│
├── .github/workflows/              # Pipeline de CI/CD
└── OffersDelivery.slnx             # Solution file
```

---

## 🚀 Como Executar Localmente

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Workload do MAUI instalado:

  ```bash
  dotnet workload install maui
  ```

- Android SDK (para build Android)
- Visual Studio 2022/2026 ou VS Code com extensões C# / MAUI

### Build e execução

```bash
# Clonar o repositório
git clone https://github.com/Pira4Ever/offers-delivery-app.git
cd offers-delivery-app

# Restaurar dependências
dotnet restore

# Executar no Android (emulador ou dispositivo)
dotnet build -t:Run -f net10.0-android
```

> **Nota:** O projeto está atualmente configurado prioritariamente para Android (`net10.0-android`).

---

## 📱 Instalação do APK

Releases automáticas são geradas a cada push na branch `main`.

Você pode baixar o APK mais recente em:

**[Releases do GitHub](https://github.com/Pira4Ever/offers-delivery-app/releases)**

O app possui sistema de atualização interna que verifica a versão disponível no GitHub e permite instalar a nova versão diretamente.

---

## 🔄 CI/CD

O workflow `.github/workflows/main.yml` realiza:

1. Build do projeto em Release
2. Assinatura do APK com keystore
3. Extração da versão do `.csproj`
4. Criação automática de Release no GitHub contendo:
   - `OffersDelivery.apk`
   - `version.txt`

---

## 🗄️ Banco de Dados Local

O aplicativo utiliza SQLite com a tabela `Offers`:

| Campo       | Tipo    | Descrição                          |
|-------------|---------|------------------------------------|
| Id          | string  | Hash único da oferta               |
| Market      | int     | Código do mercado (enum)           |
| Type        | int     | 0 = PDF / 1 = Imagem               |
| Url         | string  | URL do arquivo                     |
| DueDate     | string  | Data de validade (yyyy-MM-dd)      |
| PageOrder   | int     | Ordem da página (folhetos imagem)  |
| OfferGroup  | string  | Agrupamento de páginas             |

Ofertas com data de validade inferior a ontem são removidas automaticamente.

---

## ⚠️ Avisos Importantes

- O scraping depende da estrutura atual dos sites dos supermercados. Mudanças nos sites podem quebrar a coleta de dados.
- Alguns sites possuem validação de certificado SSL customizada (ex: Roldão).
- O aplicativo é focado na região de **Salto/SP** (filtros específicos por loja/cidade em alguns scrapers).
- Uso apenas para fins educacionais e pessoais. Respeite os termos de uso dos sites originais.

---

## 📄 Licença

Este projeto está licenciado sob a **GNU General Public License v3.0**.

Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 👨‍💻 Autor

Desenvolvido por **[Pira4Ever](https://github.com/Pira4Ever)**

---

## 📌 Roadmap / Possíveis Melhorias

- [ ] Suporte a mais mercados
- [ ] Notificações de novas ofertas
- [ ] Filtro por categoria de produto
- [ ] Modo offline aprimorado
- [ ] Versão iOS estável
- [ ] Busca por produto dentro dos folhetos

---

⭐ Se este projeto foi útil para você, deixe uma estrela no repositório!
