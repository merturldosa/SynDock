"use client";

import { useState } from "react";
import { Share2, Check } from "lucide-react";

interface ShareButtonProps {
  title: string;
  text?: string;
  url?: string;
}

export function ShareButton({ title, text, url }: ShareButtonProps) {
  const [copied, setCopied] = useState(false);

  const handleShare = async () => {
    const shareUrl = url || window.location.href;

    if (navigator.share) {
      try {
        await navigator.share({ title, text, url: shareUrl });
      } catch {
        /* user cancelled */
      }
    } else {
      try {
        await navigator.clipboard.writeText(shareUrl);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
      } catch {
        /* clipboard failed */
      }
    }
  };

  return (
    <button
      onClick={handleShare}
      className="p-4 rounded-xl border-2 border-gray-200 text-gray-400 hover:border-blue-300 hover:text-blue-500 transition-colors"
      title="공유하기"
    >
      {copied ? <Check size={22} className="text-emerald-500" /> : <Share2 size={22} />}
    </button>
  );
}
