"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";

const DEFAULT_REACTION_KEYS = [
  { emoji: "🙏", key: "prayer" },
  { emoji: "✝️", key: "amen" },
  { emoji: "❤️", key: "love" },
  { emoji: "🕊️", key: "blessing" },
  { emoji: "🙌", key: "thanks" },
  { emoji: "⭐", key: "hallelujah" },
];

interface ReactionPickerProps {
  reactions?: string[];
  currentReaction?: string | null;
  reactionCount: number;
  onReact: (reactionType: string) => void;
  disabled?: boolean;
}

export function ReactionPicker({
  reactions,
  currentReaction,
  reactionCount,
  onReact,
  disabled = false,
}: ReactionPickerProps) {
  const t = useTranslations();
  const [showPicker, setShowPicker] = useState(false);

  const DEFAULT_REACTIONS = DEFAULT_REACTION_KEYS.map((r) => ({
    emoji: r.emoji,
    label: t(`reaction.${r.key}`),
    key: r.key,
  }));

  const reactionList = reactions
    ? reactions.map((r) => {
        const found = DEFAULT_REACTIONS.find((d) => d.label === r || d.emoji === r);
        return found || { emoji: r, label: r, key: r };
      })
    : DEFAULT_REACTIONS;

  const activeReaction = currentReaction
    ? reactionList.find((r) => r.label === currentReaction || r.emoji === currentReaction)
    : null;

  return (
    <div className="relative">
      <button
        onClick={() => {
          if (disabled) return;
          if (activeReaction) {
            onReact(activeReaction.label);
          } else {
            setShowPicker(!showPicker);
          }
        }}
        disabled={disabled}
        className={`flex items-center gap-1 text-sm transition-colors ${
          activeReaction
            ? "text-[var(--color-primary)]"
            : "text-gray-400 hover:text-[var(--color-primary)]"
        }`}
      >
        <span className="text-base">{activeReaction?.emoji || "🙏"}</span>
        <span>{reactionCount}</span>
      </button>

      {showPicker && (
        <div className="absolute bottom-full left-0 mb-2 bg-white rounded-xl shadow-lg border border-gray-200 p-2 flex gap-1 z-50">
          {reactionList.map((r) => (
            <button
              key={r.label}
              onClick={() => {
                onReact(r.label);
                setShowPicker(false);
              }}
              className="flex flex-col items-center p-2 rounded-lg hover:bg-gray-100 transition-colors min-w-[48px]"
              title={r.label}
            >
              <span className="text-xl">{r.emoji}</span>
              <span className="text-[10px] text-gray-400 mt-0.5">{r.label}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
