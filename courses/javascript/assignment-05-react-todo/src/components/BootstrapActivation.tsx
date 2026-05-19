"use client";

import { useEffect } from "react";

export default function BootstrapActivation() {
  useEffect(() => {
    import("bootstrap/dist/js/bootstrap.bundle.min.js");
  }, []);
  return null;
}
