variable env_id {
    type = string
    description = "The environment identifier (e.g., dev, staging, prod)"
    default = "dev"
}

variable src_key {
    type = string
    description = "The infrastructure source"
    default = "terraform"
}

variable subscriotion_id {
  type = string
  description = "Azure Subscription ID"
  default = "3b3ba661-a188-44c2-abf7-8684f22b665e"
}

variable pg-sql-pwd {
  type = string
  default = "Pgsql@12345"
  description = "Postgres Sql Password"

}