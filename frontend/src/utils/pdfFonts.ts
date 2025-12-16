import { Font } from '@react-pdf/renderer';

// Use window object to persist font registration state across HMR
// This ensures fonts are only registered once even if modules reload
declare global {
  interface Window {
    __pdfFontsRegistered?: Set<string>;
  }
}

/**
 * Get or create the global font registration tracker
 */
function getRegisteredFonts(): Set<string> {
  if (!window.__pdfFontsRegistered) {
    window.__pdfFontsRegistered = new Set<string>();
  }
  return window.__pdfFontsRegistered;
}

/**
 * Register NotoSans font for PDF rendering (supports Unicode characters like ₹)
 * This function is idempotent - it will only register the font once globally
 */
export function registerNotoSansFont(): void {
  const fontFamily = 'NotoSans';
  const registeredFonts = getRegisteredFonts();
  
  if (registeredFonts.has(fontFamily)) {
    return;
  }

  try {
    Font.register({
      family: fontFamily,
      fonts: [
        { src: '/fonts/NotoSans-Regular.ttf', fontWeight: 'normal' },
        { src: '/fonts/NotoSans-Bold.ttf', fontWeight: 'bold' },
        { src: '/fonts/NotoSans-Italic.ttf', fontStyle: 'italic' },
      ],
    });
    
    registeredFonts.add(fontFamily);
  } catch (error) {
    // Font might already be registered internally, mark as registered to prevent retries
    registeredFonts.add(fontFamily);
    // Silently ignore - the font is likely already registered
  }
}

/**
 * Register Nunito font for PDF rendering
 * This function is idempotent - it will only register the font once globally
 */
export function registerNunitoFont(): void {
  const fontFamily = 'Nunito';
  const registeredFonts = getRegisteredFonts();
  
  if (registeredFonts.has(fontFamily)) {
    return;
  }

  try {
    Font.register({
      family: fontFamily,
      fonts: [
        { src: 'https://fonts.gstatic.com/s/nunito/v12/XRXV3I6Li01BKofINeaE.ttf' },
        {
          src: 'https://fonts.gstatic.com/s/nunito/v12/XRXW3I6Li01BKofA6sKUYevN.ttf',
          fontWeight: 600,
        },
      ],
    });
    
    registeredFonts.add(fontFamily);
  } catch (error) {
    // Font might already be registered internally, mark as registered to prevent retries
    registeredFonts.add(fontFamily);
    // Silently ignore - the font is likely already registered
  }
}

