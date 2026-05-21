import { EnrollmentDetailView } from "@/features/enrollment/enrollment-detail-view";

type Props = { params: { code: string } };

export default function RiskEnrollmentDetailPage({ params }: Props) {
  return <EnrollmentDetailView code={decodeURIComponent(params.code)} />;
}
