import { useEffect, useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Download, ExternalLink, Copy, Check } from 'lucide-react';
import toast from 'react-hot-toast';

interface QrCodeDisplayProps {
  qrCodeData: string | null | undefined;
  irn?: string | null;
  signedInvoice?: string | null;
  invoiceNumber?: string;
  className?: string;
  size?: 'sm' | 'md' | 'lg';
}

const SIZE_MAP = {
  sm: 120,
  md: 180,
  lg: 256,
};

export function QrCodeDisplay({
  qrCodeData,
  irn,
  signedInvoice,
  invoiceNumber,
  className,
  size = 'md',
}: QrCodeDisplayProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [copied, setCopied] = useState(false);
  const [imageUrl, setImageUrl] = useState<string | null>(null);

  // Generate QR code using a simple library or inline SVG
  useEffect(() => {
    if (!qrCodeData || !canvasRef.current) return;

    // For base64 encoded QR image from IRP
    if (qrCodeData.startsWith('data:image') || qrCodeData.length > 500) {
      setImageUrl(qrCodeData.startsWith('data:image') ? qrCodeData : `data:image/png;base64,${qrCodeData}`);
      return;
    }

    // For raw QR data, we would need a QR code generation library
    // For now, display a placeholder
    setImageUrl(null);
  }, [qrCodeData]);

  const handleCopyIrn = async () => {
    if (!irn) return;
    try {
      await navigator.clipboard.writeText(irn);
      setCopied(true);
      toast.success('IRN copied to clipboard');
      setTimeout(() => setCopied(false), 2000);
    } catch {
      toast.error('Failed to copy IRN');
    }
  };

  const handleDownloadQr = () => {
    if (!imageUrl) return;

    const link = document.createElement('a');
    link.download = `einvoice-qr-${invoiceNumber || 'download'}.png`;
    link.href = imageUrl;
    link.click();
  };

  const handleViewSignedInvoice = () => {
    if (!signedInvoice) return;

    try {
      const parsed = JSON.parse(signedInvoice);
      const blob = new Blob([JSON.stringify(parsed, null, 2)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
    } catch {
      // If not JSON, try to display as-is
      const blob = new Blob([signedInvoice], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
    }
  };

  if (!qrCodeData) {
    return null;
  }

  const qrSize = SIZE_MAP[size];

  return (
    <Card className={className}>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium">E-Invoice QR Code</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* QR Code Image */}
        <div className="flex justify-center">
          {imageUrl ? (
            <img
              src={imageUrl}
              alt="E-Invoice QR Code"
              width={qrSize}
              height={qrSize}
              className="border rounded-lg"
            />
          ) : (
            <canvas
              ref={canvasRef}
              width={qrSize}
              height={qrSize}
              className="border rounded-lg bg-gray-50"
            />
          )}
        </div>

        {/* IRN Display */}
        {irn && (
          <div className="space-y-1">
            <div className="text-xs text-gray-500 font-medium">Invoice Reference Number (IRN)</div>
            <div className="flex items-center gap-2">
              <code className="flex-1 text-xs font-mono bg-gray-100 dark:bg-gray-800 p-2 rounded break-all">
                {irn}
              </code>
              <Button variant="ghost" size="sm" onClick={handleCopyIrn}>
                {copied ? <Check className="h-4 w-4 text-green-500" /> : <Copy className="h-4 w-4" />}
              </Button>
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex gap-2">
          {imageUrl && (
            <Button variant="outline" size="sm" onClick={handleDownloadQr}>
              <Download className="h-4 w-4 mr-1" />
              Download QR
            </Button>
          )}
          {signedInvoice && (
            <Button variant="outline" size="sm" onClick={handleViewSignedInvoice}>
              <ExternalLink className="h-4 w-4 mr-1" />
              View Signed Invoice
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
