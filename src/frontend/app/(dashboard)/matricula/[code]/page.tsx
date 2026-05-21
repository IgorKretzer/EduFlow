import { redirect } from "next/navigation";

type Props = { params: { code: string } };

export default function MatriculaRedirectPage({ params }: Props) {
  redirect(`/risk-enrollments/${encodeURIComponent(params.code)}`);
}
