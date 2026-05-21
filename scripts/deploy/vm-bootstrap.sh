#!/usr/bin/env bash
# Bootstrap rápido em Ubuntu 22/24 (Oracle Cloud Always Free, etc.).
set -euo pipefail

echo "=== EduFlow — bootstrap VM Ubuntu ==="

sudo apt-get update -y
sudo apt-get install -y ca-certificates curl git

if ! command -v docker >/dev/null 2>&1; then
  sudo apt-get install -y docker.io docker-compose-v2
  sudo usermod -aG docker "$USER" || true
fi

sudo systemctl enable docker
sudo systemctl start docker

echo ""
echo "Próximos passos (como usuário com grupo docker, ou após logout/login):"
echo "  git clone <seu-repo> eduflow && cd eduflow"
echo "  cp deploy/.env.pilot.example deploy/.env.pilot"
echo "  # edite senhas, Jwt__Secret, Cors__Origins__0 (URL Vercel), API_DOMAIN"
echo "  chmod +x scripts/deploy/*.sh"
echo "  ./scripts/deploy/init-pilot.sh"
echo ""
echo "Firewall (Oracle): liberar ingress TCP 80 e 443 na Security List / NSG."
