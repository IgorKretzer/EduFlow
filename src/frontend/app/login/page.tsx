import { LoginForm } from "@/features/login/login-form";
import { LoginHero } from "@/features/login/login-hero";
import { ThemeToggle } from "@/components/theme/theme-toggle";

export default function LoginPage() {
  return (
    <div className="relative flex min-h-screen">
      <div className="absolute right-4 top-4 z-10">
        <ThemeToggle showLabel />
      </div>
      <LoginHero />
      <LoginForm />
    </div>
  );
}
