-- Create WorkflowStatus table manually
CREATE TABLE IF NOT EXISTS halulu_api.workflow_statuses (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "RequestId" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "IsInProgress" boolean NOT NULL DEFAULT false,
    "InProgressDate" timestamp with time zone,
    "IsCheckedIn" boolean NOT NULL DEFAULT false,
    "CheckedInDate" timestamp with time zone,
    "IsCompleted" boolean NOT NULL DEFAULT false,
    "CompletedDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_workflow_statuses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_workflow_statuses_service_requests_RequestId" FOREIGN KEY ("RequestId") REFERENCES halulu_api.service_requests ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_workflow_statuses_providers_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES halulu_api.providers ("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_workflow_statuses_RequestId_ProviderId" ON halulu_api.workflow_statuses ("RequestId", "ProviderId");
CREATE INDEX IF NOT EXISTS "IX_workflow_statuses_RequestId" ON halulu_api.workflow_statuses ("RequestId");
CREATE INDEX IF NOT EXISTS "IX_workflow_statuses_ProviderId" ON halulu_api.workflow_statuses ("ProviderId");